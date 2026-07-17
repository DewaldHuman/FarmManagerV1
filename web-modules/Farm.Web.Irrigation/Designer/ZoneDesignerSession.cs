using Farm.Web.Core.Irrigation;

namespace Farm.Web.Irrigation.Designer;

public enum DesignerTool
{
    Select,
    Pipe,
    Pump,
    Sprinkler,
    Valve,
    Source,
}

/// <summary>
/// The Zone Designer's interaction state machine — tools, selection, pipe
/// drawing, and the three drag modes (element move, whole-pipe translate,
/// endpoint resize). Plain C#: coordinates are in metres (the page converts
/// pixels), no Blazor types, so the whole surface is unit-testable.
/// Mutating methods return true when the design changed and should be saved.
/// </summary>
public class ZoneDesignerSession
{
    public ZoneDesign Design { get; set; } = new();

    public DesignerTool Tool { get; private set; } = DesignerTool.Select;

    public object? Selected { get; private set; }

    public DesignPipe? DrawingPipe { get; private set; }

    public bool IsDragging => _dragTarget is not null;

    // Drag-to-move (Select tool). The page renders a full-canvas overlay while
    // IsDragging so pointer offsets stay in SVG space regardless of the target.
    private object? _dragTarget;
    private bool _dragStarted;
    private double _dragGrabDx, _dragGrabDy; // sprinkler/point: element pos − pointer pos
    private double _pipeAnchorX, _pipeAnchorY; // pipe: last applied (snapped) pointer

    // Endpoint resize: non-null switches the drag into vertex mode — the endpoint
    // slides along its adjacent segment's axis (straight/90° runs preserved).
    private DesignVertex? _dragVertex;
    private DesignVertex? _dragVertexNeighbor;

    // ---- Tools ----

    public bool SetTool(DesignerTool tool)
    {
        var changed = false;
        if (Tool == DesignerTool.Pipe && tool != DesignerTool.Pipe)
        {
            changed = FinishPipe();
        }

        Tool = tool;
        return changed;
    }

    // ---- Canvas clicks (placement / drawing / deselect) ----

    public bool CanvasClick(double xMetres, double yMetres)
    {
        var (x, y) = SnapClamp(xMetres, yMetres);
        switch (Tool)
        {
            case DesignerTool.Select:
                Selected = null;
                return false;
            case DesignerTool.Pipe:
                AddPipeVertex(x, y);
                return false; // the pipe only lands in the design when finished
            case DesignerTool.Pump:
                Design.Points.Add(new DesignPoint { Kind = DesignPointKind.Pump, X = x, Y = y });
                return true;
            case DesignerTool.Sprinkler:
                Design.Sprinklers.Add(new DesignSprinkler { X = x, Y = y });
                return true;
            case DesignerTool.Valve:
                Design.Points.Add(new DesignPoint { Kind = DesignPointKind.Valve, X = x, Y = y });
                return true;
            case DesignerTool.Source:
                Design.Points.Add(new DesignPoint { Kind = DesignPointKind.WaterSource, X = x, Y = y });
                return true;
            default:
                return false;
        }
    }

    public bool CanvasDoubleClick() => Tool == DesignerTool.Pipe && FinishPipe();

    private void AddPipeVertex(double x, double y)
    {
        DrawingPipe ??= new DesignPipe();
        if (DrawingPipe.Vertices.Count > 0)
        {
            // Straight/90° segments only: constrain to the dominant axis.
            var last = DrawingPipe.Vertices[^1];
            if (Math.Abs(x - last.X) >= Math.Abs(y - last.Y))
            {
                y = last.Y;
            }
            else
            {
                x = last.X;
            }

            if (x == last.X && y == last.Y)
            {
                return; // duplicate click (the first half of a double-click lands here too)
            }
        }

        DrawingPipe.Vertices.Add(new DesignVertex { X = x, Y = y });
    }

    private bool FinishPipe()
    {
        if (DrawingPipe is not null && DrawingPipe.Vertices.Count >= 2)
        {
            // First pipe drawn becomes the main line; later pipes are laterals.
            DrawingPipe.IsMain = !Design.Pipes.Any(p => p.IsMain);
            Design.Pipes.Add(DrawingPipe);
            DrawingPipe = null;
            return true;
        }

        DrawingPipe = null;
        return false;
    }

    // ---- Selection ----

    public void SelectElement(object element)
    {
        if (Tool == DesignerTool.Select)
        {
            Selected = element;
        }
    }

    public void ClearSelection() => Selected = null;

    public bool RemoveSelected()
    {
        var removed = Selected switch
        {
            DesignPipe pipe => Design.Pipes.Remove(pipe),
            DesignSprinkler sprinkler => Design.Sprinklers.Remove(sprinkler),
            DesignPoint point => Design.Points.Remove(point),
            _ => false,
        };

        Selected = null;
        return removed;
    }

    public bool TogglePipeMain()
    {
        if (Selected is not DesignPipe pipe)
        {
            return false;
        }

        if (pipe.IsMain)
        {
            // Demoting may leave no main — the "No main pipe" warning covers that.
            pipe.IsMain = false;
        }
        else
        {
            // Single-main invariant: demote the current main, promote this one.
            foreach (var other in Design.Pipes.Where(p => p.IsMain))
            {
                other.IsMain = false;
            }

            pipe.IsMain = true;
        }

        return true;
    }

    // ---- Drag-to-move / endpoint resize ----

    public void BeginDrag(object element)
    {
        if (Tool != DesignerTool.Select)
        {
            return;
        }

        Selected = element;
        _dragTarget = element;
        _dragStarted = false;
    }

    public void BeginVertexDrag(DesignPipe pipe, bool endVertex)
    {
        if (Tool != DesignerTool.Select || pipe.Vertices.Count < 2)
        {
            return;
        }

        Selected = pipe;
        _dragTarget = pipe; // makes IsDragging true → page renders the overlay
        _dragVertex = endVertex ? pipe.Vertices[^1] : pipe.Vertices[0];
        _dragVertexNeighbor = endVertex ? pipe.Vertices[^2] : pipe.Vertices[1];
    }

    public void DragMove(double xMetres, double yMetres)
    {
        if (_dragTarget is null)
        {
            return;
        }

        if (_dragVertex is not null && _dragVertexNeighbor is not null)
        {
            // Endpoint resize: slide along the adjacent segment's axis only,
            // never landing on the neighbor (min 1 m segment).
            var (sx, sy) = SnapClamp(xMetres, yMetres);
            if (_dragVertex.Y == _dragVertexNeighbor.Y)
            {
                if (sx != _dragVertexNeighbor.X)
                {
                    _dragVertex.X = sx;
                }
            }
            else if (sy != _dragVertexNeighbor.Y)
            {
                _dragVertex.Y = sy;
            }

            return;
        }

        if (!_dragStarted)
        {
            // First move: record grab offsets so the element doesn't jump under the pointer.
            switch (_dragTarget)
            {
                case DesignSprinkler s:
                    _dragGrabDx = s.X - xMetres;
                    _dragGrabDy = s.Y - yMetres;
                    break;
                case DesignPoint p:
                    _dragGrabDx = p.X - xMetres;
                    _dragGrabDy = p.Y - yMetres;
                    break;
                case DesignPipe:
                    _pipeAnchorX = Math.Round(xMetres);
                    _pipeAnchorY = Math.Round(yMetres);
                    break;
            }

            _dragStarted = true;
            return;
        }

        switch (_dragTarget)
        {
            case DesignSprinkler s:
                (s.X, s.Y) = SnapClamp(xMetres + _dragGrabDx, yMetres + _dragGrabDy);
                break;
            case DesignPoint p:
                (p.X, p.Y) = SnapClamp(xMetres + _dragGrabDx, yMetres + _dragGrabDy);
                break;
            case DesignPipe pipe:
                var dx = Math.Round(xMetres) - _pipeAnchorX;
                var dy = Math.Round(yMetres) - _pipeAnchorY;
                if (dx != 0 || dy != 0)
                {
                    // Clamp the translation so no vertex leaves the canvas.
                    dx = Math.Clamp(dx, -pipe.Vertices.Min(v => v.X), Design.WidthMetres - pipe.Vertices.Max(v => v.X));
                    dy = Math.Clamp(dy, -pipe.Vertices.Min(v => v.Y), Design.HeightMetres - pipe.Vertices.Max(v => v.Y));
                    foreach (var v in pipe.Vertices)
                    {
                        v.X += dx;
                        v.Y += dy;
                    }

                    _pipeAnchorX += dx;
                    _pipeAnchorY += dy;
                }
                break;
        }
    }

    public bool DragEnd()
    {
        if (_dragTarget is null)
        {
            return false;
        }

        _dragTarget = null;
        _dragVertex = null;
        _dragVertexNeighbor = null;
        return true; // save + recompute (sprinkler→lateral assignment may change)
    }

    private (double X, double Y) SnapClamp(double x, double y) =>
        (Math.Clamp(Math.Round(x), 0, Design.WidthMetres), Math.Clamp(Math.Round(y), 0, Design.HeightMetres));
}
