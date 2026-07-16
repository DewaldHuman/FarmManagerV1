namespace Farm.Web.Core.Irrigation;

// Client-side irrigation layout model for the Zone Designer. Lives in
// Farm.Web.Core (not the lazy Farm.Web.Irrigation) because ZoneOverview.razor
// needs to read design status — same eager-assembly rule as IIrrigationService.
// Mutable classes: the designer canvas edits these in place. All coordinates
// and lengths are in metres.

public enum DesignPointKind
{
    Pump,
    Valve,
    WaterSource,
}

public class DesignVertex
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class DesignPipe
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsMain { get; set; }
    public List<DesignVertex> Vertices { get; set; } = new();

    /// <summary>Per-pipe override; null inherits the design's main/lateral diameter input.</summary>
    public double? DiameterMm { get; set; }

    public double LengthMetres
    {
        get
        {
            double total = 0;
            for (var i = 1; i < Vertices.Count; i++)
            {
                total += Math.Abs(Vertices[i].X - Vertices[i - 1].X)
                       + Math.Abs(Vertices[i].Y - Vertices[i - 1].Y);
            }
            return total;
        }
    }

    /// <summary>Shortest distance from a point to this pipe's polyline (metres).</summary>
    public double DistanceToPoint(double x, double y)
    {
        if (Vertices.Count == 0)
        {
            return double.MaxValue;
        }

        var best = double.MaxValue;
        for (var i = 0; i < Vertices.Count; i++)
        {
            double d;
            if (i == Vertices.Count - 1)
            {
                d = Math.Sqrt(Math.Pow(x - Vertices[i].X, 2) + Math.Pow(y - Vertices[i].Y, 2));
            }
            else
            {
                var (ax, ay) = (Vertices[i].X, Vertices[i].Y);
                var (bx, by) = (Vertices[i + 1].X, Vertices[i + 1].Y);
                var (dx, dy) = (bx - ax, by - ay);
                var lengthSq = dx * dx + dy * dy;
                var t = lengthSq == 0 ? 0 : Math.Clamp(((x - ax) * dx + (y - ay) * dy) / lengthSq, 0, 1);
                d = Math.Sqrt(Math.Pow(x - (ax + t * dx), 2) + Math.Pow(y - (ay + t * dy), 2));
            }

            best = Math.Min(best, d);
        }

        return best;
    }
}

public class DesignSprinkler
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public double X { get; set; }
    public double Y { get; set; }

    // Per-sprinkler overrides; null inherits the design inputs.
    public double? FlowLitresPerHour { get; set; }
    public double? RadiusMetres { get; set; }
}

public class DesignPoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DesignPointKind Kind { get; set; }
    public double X { get; set; }
    public double Y { get; set; }

    // Pump-only overrides; null inherits the design inputs. Ignored for valve/source.
    public double? RatedFlowLitresPerMinute { get; set; }
    public double? RatedPressureBar { get; set; }
}

/// <summary>Editable design inputs; defaults match the reference mockup.</summary>
public class DesignInputs
{
    public double PumpFlowLitresPerMinute { get; set; } = 380;
    public double PumpPressureBar { get; set; } = 3.5;
    public double MainDiameterMm { get; set; } = 50;
    public double LateralDiameterMm { get; set; } = 25;
    public int SprinklerCount { get; set; } = 15;
    public double SprinklerFlowLitresPerHour { get; set; } = 200;
    public double SprinklerSpacingMetres { get; set; } = 12;
    public double SprinklerRadiusMetres { get; set; } = 7;
    public double TargetDepthMm { get; set; } = 12;
}

public class ZoneDesign
{
    public double WidthMetres { get; set; } = 80;
    public double HeightMetres { get; set; } = 56;
    public List<DesignPipe> Pipes { get; set; } = new();
    public List<DesignSprinkler> Sprinklers { get; set; } = new();
    public List<DesignPoint> Points { get; set; } = new();
    public DesignInputs Inputs { get; set; } = new();

    public bool IsEmpty => Pipes.Count == 0 && Sprinklers.Count == 0 && Points.Count == 0;
}
