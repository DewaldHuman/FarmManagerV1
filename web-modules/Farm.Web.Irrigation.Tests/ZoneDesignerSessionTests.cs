using Farm.Web.Core.Irrigation;
using Farm.Web.Irrigation.Designer;

namespace Farm.Web.Irrigation.Tests;

public class ZoneDesignerSessionTests
{
    private static ZoneDesignerSession NewSession(DesignerTool tool = DesignerTool.Select)
    {
        var session = new ZoneDesignerSession();
        session.SetTool(tool);
        return session;
    }

    // ---- Pipe drawing ----

    [Fact]
    public void PipeDrawing_ClickAddsVertex_DoubleClickFinishes()
    {
        var session = NewSession(DesignerTool.Pipe);
        session.CanvasClick(10, 10);
        session.CanvasClick(40, 10);
        Assert.Equal(2, session.DrawingPipe!.Vertices.Count);

        var changed = session.CanvasDoubleClick();

        Assert.True(changed);
        Assert.Null(session.DrawingPipe);
        var pipe = Assert.Single(session.Design.Pipes);
        Assert.Equal(30, pipe.LengthMetres);
    }

    [Fact]
    public void PipeDrawing_ConstrainsToDominantAxis()
    {
        var session = NewSession(DesignerTool.Pipe);
        session.CanvasClick(10, 10);
        session.CanvasClick(40, 22); // dx=30 > dy=12 → snaps to horizontal
        session.CanvasDoubleClick();

        var vertex = session.Design.Pipes[0].Vertices[1];
        Assert.Equal(40, vertex.X);
        Assert.Equal(10, vertex.Y);
    }

    [Fact]
    public void PipeDrawing_DuplicateClickIgnored()
    {
        var session = NewSession(DesignerTool.Pipe);
        session.CanvasClick(10, 10);
        session.CanvasClick(10, 10); // double-click's first half lands here too

        Assert.Single(session.DrawingPipe!.Vertices);
    }

    [Fact]
    public void PipeDrawing_SingleVertexPipe_IsDiscardedOnFinish()
    {
        var session = NewSession(DesignerTool.Pipe);
        session.CanvasClick(10, 10);

        var changed = session.CanvasDoubleClick();

        Assert.False(changed);
        Assert.Null(session.DrawingPipe);
        Assert.Empty(session.Design.Pipes);
    }

    [Fact]
    public void PipeDrawing_FirstPipeIsMain_SecondIsLateral()
    {
        var session = NewSession(DesignerTool.Pipe);
        session.CanvasClick(5, 5);
        session.CanvasClick(5, 40);
        session.CanvasDoubleClick();
        session.CanvasClick(5, 10);
        session.CanvasClick(50, 10);
        session.CanvasDoubleClick();

        Assert.True(session.Design.Pipes[0].IsMain);
        Assert.False(session.Design.Pipes[1].IsMain);
    }

    [Fact]
    public void SetTool_AwayFromPipe_FinishesTheDrawingPipe()
    {
        var session = NewSession(DesignerTool.Pipe);
        session.CanvasClick(10, 10);
        session.CanvasClick(30, 10);

        var changed = session.SetTool(DesignerTool.Select);

        Assert.True(changed);
        Assert.Single(session.Design.Pipes);
    }

    // ---- Placement ----

    [Fact]
    public void Placement_SnapsAndClamps()
    {
        var session = NewSession(DesignerTool.Sprinkler);
        session.CanvasClick(10.4, 10.6);        // snaps to 10, 11
        session.CanvasClick(999, -5);           // clamps to canvas (80×56 default)

        Assert.Equal((10.0, 11.0), (session.Design.Sprinklers[0].X, session.Design.Sprinklers[0].Y));
        Assert.Equal((80.0, 0.0), (session.Design.Sprinklers[1].X, session.Design.Sprinklers[1].Y));
    }

    [Fact]
    public void Placement_EachToolAddsItsElement()
    {
        var session = NewSession(DesignerTool.Pump);
        Assert.True(session.CanvasClick(5, 5));
        session.SetTool(DesignerTool.Valve);
        Assert.True(session.CanvasClick(6, 6));
        session.SetTool(DesignerTool.Source);
        Assert.True(session.CanvasClick(7, 7));

        Assert.Equal(
            new[] { DesignPointKind.Pump, DesignPointKind.Valve, DesignPointKind.WaterSource },
            session.Design.Points.Select(p => p.Kind).ToArray());
    }

    // ---- Selection ----

    [Fact]
    public void Selection_OnlyInSelectTool_AndCanvasClickDeselects()
    {
        var session = NewSession(DesignerTool.Sprinkler);
        session.CanvasClick(10, 10);
        var sprinkler = session.Design.Sprinklers[0];

        session.SelectElement(sprinkler); // wrong tool → ignored
        Assert.Null(session.Selected);

        session.SetTool(DesignerTool.Select);
        session.SelectElement(sprinkler);
        Assert.Same(sprinkler, session.Selected);

        session.CanvasClick(30, 30); // background click deselects
        Assert.Null(session.Selected);
    }

    [Fact]
    public void RemoveSelected_RemovesAndClearsSelection()
    {
        var session = NewSession(DesignerTool.Sprinkler);
        session.CanvasClick(10, 10);
        session.SetTool(DesignerTool.Select);
        session.SelectElement(session.Design.Sprinklers[0]);

        Assert.True(session.RemoveSelected());
        Assert.Empty(session.Design.Sprinklers);
        Assert.Null(session.Selected);
        Assert.False(session.RemoveSelected()); // nothing selected → no change
    }

    // ---- Drag ----

    [Fact]
    public void Drag_MovesElementWithGrabOffset_SnappedAndClamped()
    {
        var session = NewSession(DesignerTool.Sprinkler);
        session.CanvasClick(10, 10);
        var sprinkler = session.Design.Sprinklers[0];
        session.SetTool(DesignerTool.Select);

        session.BeginDrag(sprinkler);
        Assert.True(session.IsDragging);
        session.DragMove(10.3, 10.2);  // first move records grab offset
        session.DragMove(30.3, 20.2);  // moved +20, +10

        Assert.Equal((30.0, 20.0), (sprinkler.X, sprinkler.Y));
        Assert.True(session.DragEnd());
        Assert.False(session.IsDragging);
    }

    [Fact]
    public void Drag_PipeTranslatesWholePolyline_ClampedAtBounds()
    {
        var session = NewSession(DesignerTool.Pipe);
        session.CanvasClick(10, 10);
        session.CanvasClick(40, 10);
        session.CanvasDoubleClick();
        var pipe = session.Design.Pipes[0];
        session.SetTool(DesignerTool.Select);

        session.BeginDrag(pipe);
        session.DragMove(20, 10);   // anchor
        session.DragMove(20, 2);    // dy = -8 → clamped to -10? min Y is 10 → allowed -10; -8 ok

        Assert.Equal(2, pipe.Vertices[0].Y);
        Assert.Equal(2, pipe.Vertices[1].Y);

        session.DragMove(20, -50);  // would push above canvas → clamped at 0
        Assert.Equal(0, pipe.Vertices[0].Y);
        Assert.Equal(0, pipe.Vertices[1].Y);
        Assert.Equal(10, pipe.Vertices[0].X); // X untouched
    }

    // ---- Endpoint resize ----

    [Fact]
    public void VertexDrag_SlidesAlongSegmentAxisOnly()
    {
        var session = NewSession(DesignerTool.Pipe);
        session.CanvasClick(10, 10);
        session.CanvasClick(40, 10); // horizontal segment
        session.CanvasDoubleClick();
        var pipe = session.Design.Pipes[0];
        session.SetTool(DesignerTool.Select);

        session.BeginVertexDrag(pipe, endVertex: true);
        session.DragMove(25, 30); // diagonal pointer → only X follows

        Assert.Equal(25, pipe.Vertices[1].X);
        Assert.Equal(10, pipe.Vertices[1].Y);
    }

    [Fact]
    public void VertexDrag_RefusesCollapseOntoNeighbor()
    {
        var session = NewSession(DesignerTool.Pipe);
        session.CanvasClick(10, 10);
        session.CanvasClick(40, 10);
        session.CanvasDoubleClick();
        var pipe = session.Design.Pipes[0];
        session.SetTool(DesignerTool.Select);

        session.BeginVertexDrag(pipe, endVertex: true);
        session.DragMove(10, 10); // exactly the neighbor → refused

        Assert.Equal(40, pipe.Vertices[1].X);
    }

    // ---- Promote / demote ----

    [Fact]
    public void TogglePipeMain_PreservesSingleMainInvariant()
    {
        var session = NewSession(DesignerTool.Pipe);
        session.CanvasClick(5, 5);
        session.CanvasClick(5, 40);
        session.CanvasDoubleClick();
        session.CanvasClick(5, 10);
        session.CanvasClick(50, 10);
        session.CanvasDoubleClick();
        var main = session.Design.Pipes[0];
        var lateral = session.Design.Pipes[1];
        session.SetTool(DesignerTool.Select);

        session.SelectElement(lateral);
        Assert.True(session.TogglePipeMain()); // promote → old main demoted
        Assert.True(lateral.IsMain);
        Assert.False(main.IsMain);
        Assert.Equal(1, session.Design.Pipes.Count(p => p.IsMain));

        Assert.True(session.TogglePipeMain()); // demote → no main at all
        Assert.Equal(0, session.Design.Pipes.Count(p => p.IsMain));
    }
}
