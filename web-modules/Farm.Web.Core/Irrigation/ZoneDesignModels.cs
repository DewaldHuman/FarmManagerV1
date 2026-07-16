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
}

public class DesignSprinkler
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public double X { get; set; }
    public double Y { get; set; }
}

public class DesignPoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DesignPointKind Kind { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
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
