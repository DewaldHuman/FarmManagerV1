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
        // Main friction accumulates per segment from the pump end: take-offs at
        // 10/24/38 m carrying 3 → 2 → 1 m³/h ≈ 0.774 kPa, plus the busiest
        // lateral's Christiansen loss ≈ 4.639 kPa → ≈ 0.054 bar.
        Assert.Equal(0.05, r.FrictionLossBar, 2);
        Assert.Equal(2.05, r.RequiredPumpPressureBar, 2);
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
    public void HeadPressure_IsEditable_AndFeedsRequiredPressure()
    {
        var design = DemoDesign();
        design.Inputs.SprinklerHeadPressureBar = 3.0;

        var r = ZoneDesignEvaluator.Evaluate(design).Results!;
        Assert.Equal(r.FrictionLossBar + 3.0, r.RequiredPumpPressureBar, 6);

        design.Inputs.SprinklerHeadPressureBar = 0;
        Assert.Null(ZoneDesignEvaluator.Evaluate(design).Results);
    }

    /// <summary>Main (0,0)→(0,30); lateral A at y=10 with two sprinklers (0.4 m³/h),
    /// lateral B at y=20 with one (0.2 m³/h).</summary>
    private static ZoneDesign TwoTakeOffDesign()
    {
        var design = new ZoneDesign();
        design.Pipes.Add(new DesignPipe
        {
            IsMain = true,
            Vertices = new List<DesignVertex> { new() { X = 0, Y = 0 }, new() { X = 0, Y = 30 } },
        });
        design.Pipes.Add(new DesignPipe
        {
            Vertices = new List<DesignVertex> { new() { X = 0, Y = 10 }, new() { X = 20, Y = 10 } },
        });
        design.Pipes.Add(new DesignPipe
        {
            Vertices = new List<DesignVertex> { new() { X = 0, Y = 20 }, new() { X = 20, Y = 20 } },
        });
        design.Sprinklers.Add(new DesignSprinkler { X = 5, Y = 10 });
        design.Sprinklers.Add(new DesignSprinkler { X = 15, Y = 10 });
        design.Sprinklers.Add(new DesignSprinkler { X = 5, Y = 20 });
        return design;
    }

    [Fact]
    public void MainFriction_AccumulatesPerSegment()
    {
        var r = ZoneDesignEvaluator.Evaluate(TwoTakeOffDesign()).Results!;

        // No inlet marker → inlet is the draw-start vertex (0,0). Segments:
        // 0–10 m at 0.6 m³/h (all flow), 10–20 m at 0.2 m³/h (lateral B's share),
        // zero-flow tail 20–30 m contributes nothing. Lateral friction = the
        // worst lateral (A: 0.4 m³/h, 2 outlets over 20 m).
        var mainKpa = Farm.Irrigation.Calculators.HydraulicsCalculators.HazenWilliamsFrictionLoss(0.6, 50, 10, 150).PressureLossKpa
                    + Farm.Irrigation.Calculators.HydraulicsCalculators.HazenWilliamsFrictionLoss(0.2, 50, 10, 150).PressureLossKpa;
        var lateralKpa = Math.Max(
            Farm.Irrigation.Calculators.HydraulicsCalculators.LateralFrictionLoss(0.4, 25, 20, 150, 2).PressureLossKpa,
            Farm.Irrigation.Calculators.HydraulicsCalculators.LateralFrictionLoss(0.2, 25, 20, 150, 1).PressureLossKpa);

        Assert.Equal((mainKpa + lateralKpa) / 100, r.FrictionLossBar, 6);
    }

    [Fact]
    public void MainFriction_InletFollowsPump()
    {
        var design = TwoTakeOffDesign();
        design.Points.Add(new DesignPoint { Kind = DesignPointKind.Pump, X = 0, Y = 30 }); // far end

        var r = ZoneDesignEvaluator.Evaluate(design).Results!;

        // Inlet flips to (0,30): take-offs mirror to 10 m (lateral B) and 20 m
        // (lateral A) → segments 0–10 at 0.6 m³/h, then 10–20 at 0.4 m³/h.
        var mainKpa = Farm.Irrigation.Calculators.HydraulicsCalculators.HazenWilliamsFrictionLoss(0.6, 50, 10, 150).PressureLossKpa
                    + Farm.Irrigation.Calculators.HydraulicsCalculators.HazenWilliamsFrictionLoss(0.4, 50, 10, 150).PressureLossKpa;
        var lateralKpa = Farm.Irrigation.Calculators.HydraulicsCalculators.LateralFrictionLoss(0.4, 25, 20, 150, 2).PressureLossKpa;

        Assert.Equal((mainKpa + lateralKpa) / 100, r.FrictionLossBar, 6);
        Assert.NotEqual(
            ZoneDesignEvaluator.Evaluate(TwoTakeOffDesign()).Results!.FrictionLossBar,
            r.FrictionLossBar);
    }

    [Fact]
    public void MainFriction_NoLaterals_FallsBackToFullDemand()
    {
        var design = new ZoneDesign();
        design.Pipes.Add(new DesignPipe
        {
            IsMain = true,
            Vertices = new List<DesignVertex> { new() { X = 0, Y = 0 }, new() { X = 0, Y = 30 } },
        });
        design.Sprinklers.Add(new DesignSprinkler { X = 5, Y = 10 });
        design.Sprinklers.Add(new DesignSprinkler { X = 5, Y = 20 });

        var r = ZoneDesignEvaluator.Evaluate(design).Results!;

        // 2 × 200 L/hr = 400 L/hr = 0.4 m³/h over the full 30 m main.
        var expectedKpa = Farm.Irrigation.Calculators.HydraulicsCalculators.HazenWilliamsFrictionLoss(0.4, 50, 30, 150).PressureLossKpa;
        Assert.Equal(expectedKpa / 100, r.FrictionLossBar, 6);
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
