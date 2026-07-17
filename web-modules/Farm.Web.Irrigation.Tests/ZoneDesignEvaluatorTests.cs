using Farm.Web.Core.Irrigation;
using Farm.Web.Irrigation.Designer;

namespace Farm.Web.Irrigation.Tests;

public class ZoneDesignEvaluatorTests
{
    /// <summary>Same geometry as InMemoryZoneDesignStore's demo layout: main up the
    /// left, three 60 m laterals at y = 14/28/42, five sprinklers each at 12 m spacing.</summary>
    private static ZoneDesign DemoDesign()
    {
        var design = new ZoneDesign();
        design.Points.Add(new DesignPoint { Kind = DesignPointKind.WaterSource, X = 4, Y = 52 });
        design.Points.Add(new DesignPoint { Kind = DesignPointKind.Pump, X = 8, Y = 52 });
        design.Points.Add(new DesignPoint { Kind = DesignPointKind.Valve, X = 8, Y = 46 });
        design.Pipes.Add(new DesignPipe
        {
            IsMain = true,
            Vertices = new List<DesignVertex> { new() { X = 8, Y = 52 }, new() { X = 8, Y = 14 } },
        });
        foreach (var y in new[] { 14.0, 28.0, 42.0 })
        {
            design.Pipes.Add(new DesignPipe
            {
                Vertices = new List<DesignVertex> { new() { X = 8, Y = y }, new() { X = 68, Y = y } },
            });
            for (var i = 0; i < 5; i++)
            {
                design.Sprinklers.Add(new DesignSprinkler { X = 14 + i * 12, Y = y });
            }
        }

        return design;
    }

    [Fact]
    public void DemoLayout_ReferenceNumbers()
    {
        var eval = ZoneDesignEvaluator.Evaluate(DemoDesign());
        var r = eval.Results!;

        Assert.Empty(eval.Warnings);
        Assert.Empty(eval.DisconnectedLateralIds);
        Assert.Empty(eval.FarSprinklerIds);
        Assert.Equal(50, r.DemandFlowLitresPerMinute, 4);
        Assert.Equal(0.42, r.MainVelocity, 2);
        Assert.Equal(0.57, r.LateralVelocity, 2);
        Assert.Equal(0.06, r.FrictionLossBar, 2);
        Assert.Equal(2.06, r.RequiredPumpPressureBar, 2);
        Assert.Equal(5, r.BusiestLateralSprinklers);
        Assert.Equal(12, r.EffectiveSpacingMetres, 4);
        Assert.True(r.SpacingFromCanvas);
        Assert.Equal(1.1667, r.CoverageRatio, 4);
        Assert.Equal(518.4, r.RunTimeMinutes, 1);
        Assert.Equal(3, eval.LateralStats.Count);
        Assert.All(eval.LateralStats.Values, s => Assert.Equal(5, s.SprinklerCount));
    }

    [Fact]
    public void DisconnectedLateral_WarnedAndCarriesNoFlow()
    {
        var design = DemoDesign();
        var floater = new DesignPipe
        {
            Vertices = new List<DesignVertex> { new() { X = 76, Y = 5 }, new() { X = 76, Y = 25 } },
        };
        design.Pipes.Add(floater);

        var eval = ZoneDesignEvaluator.Evaluate(design);

        Assert.Contains(eval.Warnings, w => w.Contains("not connected"));
        Assert.Contains(floater.Id, eval.DisconnectedLateralIds);
        Assert.Equal(0, eval.LateralStats[floater.Id].SprinklerCount);
        Assert.Equal(0, eval.LateralStats[floater.Id].FlowLitresPerMinute);
        Assert.Equal(50, eval.Results!.DemandFlowLitresPerMinute, 4); // others unaffected
    }

    [Fact]
    public void NoMainPipe_WarnsAndStillProducesResults()
    {
        var design = DemoDesign();
        design.Pipes.RemoveAll(p => p.IsMain);

        var eval = ZoneDesignEvaluator.Evaluate(design);

        Assert.Contains(eval.Warnings, w => w.Contains("No main pipe"));
        Assert.NotNull(eval.Results);
        Assert.Equal(50, eval.Results!.DemandFlowLitresPerMinute, 4);
    }

    [Fact]
    public void FarSprinkler_WarnedAndFlagged()
    {
        var design = DemoDesign();
        var far = design.Sprinklers[0];
        far.Y = 21; // 7 m from both the y=14 and y=28 laterals

        var eval = ZoneDesignEvaluator.Evaluate(design);

        Assert.Contains(eval.Warnings, w => w.Contains("more than 2 m"));
        Assert.Contains(far.Id, eval.FarSprinklerIds);
    }

    [Fact]
    public void PlanningMode_UsesInputs_NoStatsNoWarnings()
    {
        var eval = ZoneDesignEvaluator.Evaluate(new ZoneDesign()); // defaults: 15 × 200 L/hr, 12 m
        var r = eval.Results!;

        Assert.Empty(eval.Warnings);
        Assert.Empty(eval.LateralStats);
        Assert.False(r.CountFromCanvas);
        Assert.False(r.SpacingFromCanvas);
        Assert.Equal(50, r.DemandFlowLitresPerMinute, 4);
        Assert.True(r.LateralCountAssumed);
        Assert.Equal(0, r.FrictionLossBar, 4); // nothing drawn
    }

    [Fact]
    public void SprinklerFlowOverride_FeedsDemand()
    {
        var design = DemoDesign();
        design.Sprinklers[0].FlowLitresPerHour = 400;

        var r = ZoneDesignEvaluator.Evaluate(design).Results!;

        // (14 × 200 + 400) ÷ 60 = 53.33 L/min
        Assert.Equal(53.3333, r.DemandFlowLitresPerMinute, 4);
        Assert.Equal(213.3333, r.AvgSprinklerFlowLitresPerHour, 4);
    }

    [Fact]
    public void MainDiameterOverride_ChangesMainVelocity()
    {
        var design = DemoDesign();
        design.Pipes.First(p => p.IsMain).DiameterMm = 63;

        var r = ZoneDesignEvaluator.Evaluate(design).Results!;

        // 3 m³/h in 63 mm: 0.000833 ÷ (π × 0.0315²) = 0.267 m/s
        Assert.Equal(0.267, r.MainVelocity, 3);
    }

    [Fact]
    public void PumpOverride_FeedsMaxSprinklersSolver()
    {
        var design = DemoDesign();
        design.Points.First(p => p.Kind == DesignPointKind.Pump).RatedFlowLitresPerMinute = 200;

        var r = ZoneDesignEvaluator.Evaluate(design).Results!;

        Assert.Equal(60, r.MaxSprinklers); // 200 × 60 ÷ 200
        Assert.Equal(200, r.PumpFlowLitresPerMinute);
    }

    [Fact]
    public void InvalidInputs_ReturnNullResults()
    {
        var design = new ZoneDesign();
        design.Inputs.TargetDepthMm = 0;

        var eval = ZoneDesignEvaluator.Evaluate(design);

        Assert.Null(eval.Results);
        Assert.Empty(eval.Warnings);
    }
}
