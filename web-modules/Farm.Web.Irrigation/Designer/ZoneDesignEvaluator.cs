using Farm.Irrigation.Calculators;
using Farm.Web.Core.Irrigation;

namespace Farm.Web.Irrigation.Designer;

public sealed record LateralStats(
    int SprinklerCount, double FlowLitresPerMinute, double Velocity, double FrictionKpa);

public sealed record DesignResults(
    int EffectiveSprinklerCount,
    bool CountFromCanvas,
    double DemandFlowLitresPerMinute,
    double LateralFlowLitresPerMinute,
    bool LateralCountAssumed,
    int BusiestLateralSprinklers,
    double MainVelocity,
    double LateralVelocity,
    double FrictionLossBar,
    double RequiredPumpPressureBar,
    double CoverageRatio,
    double ApplicationRateMmPerHour,
    double RunTimeMinutes,
    int MaxSprinklers,
    double AvgSprinklerFlowLitresPerHour,
    double PumpFlowLitresPerMinute,
    double EffectiveSpacingMetres,
    bool SpacingFromCanvas);

public sealed record DesignEvaluation(
    DesignResults? Results,
    IReadOnlyDictionary<Guid, LateralStats> LateralStats,
    IReadOnlyList<string> Warnings,
    IReadOnlySet<Guid> DisconnectedLateralIds,
    IReadOnlySet<Guid> FarSprinklerIds);

/// <summary>
/// Pure evaluation of a ZoneDesign into results, per-lateral stats, and layout
/// warnings — no I/O, no UI. Lives in the lazy Farm.Web.Irrigation module (not
/// Farm.Web.Core) because it depends on the lazy-loaded Farm.Irrigation.Calculators
/// assembly, which Core must never reference.
/// </summary>
public static class ZoneDesignEvaluator
{
    private const double LitresPerMinuteToM3PerHour = 0.06;
    private const double SprinklerHeadPressureBar = 2.0;
    private const int AssumedLateralCount = 3; // matches the demo layout when nothing is drawn

    // Connectivity tolerances (grid is 1 m): a lateral counts as plumbed into the
    // main within one cell; a sprinkler counts as sitting on its lateral within two.
    public const double LateralConnectToleranceM = 1.0;
    public const double SprinklerOnLateralToleranceM = 2.0;

    public static DesignEvaluation Evaluate(ZoneDesign design)
    {
        var lateralStats = new Dictionary<Guid, LateralStats>();
        var warnings = new List<string>();
        var disconnectedLateralIds = new HashSet<Guid>();
        var farSprinklerIds = new HashSet<Guid>();
        var i = design.Inputs;

        // Sprinklers placed on the canvas take precedence over the count input;
        // the input only applies while the layout has none (planning mode).
        var countFromCanvas = design.Sprinklers.Count > 0;
        var effectiveCount = countFromCanvas ? design.Sprinklers.Count : i.SprinklerCount;

        if (i.PumpFlowLitresPerMinute <= 0 || i.MainDiameterMm <= 0 || i.LateralDiameterMm <= 0
            || effectiveCount <= 0 || i.SprinklerFlowLitresPerHour <= 0
            || i.SprinklerSpacingMetres <= 0 || i.SprinklerRadiusMetres <= 0 || i.TargetDepthMm <= 0)
        {
            return new DesignEvaluation(null, lateralStats, warnings, disconnectedLateralIds, farSprinklerIds);
        }

        // Per-element overrides: null inherits the matching design input.
        double SprinklerFlowLhr(DesignSprinkler s) => s.FlowLitresPerHour ?? i.SprinklerFlowLitresPerHour;
        double PipeDiameterMm(DesignPipe p) => p.DiameterMm ?? (p.IsMain ? i.MainDiameterMm : i.LateralDiameterMm);

        var demandLmin = countFromCanvas
            ? design.Sprinklers.Sum(SprinklerFlowLhr) / 60
            : ZoneDesignCalculators.DemandFlowLitresPerMinute(effectiveCount, i.SprinklerFlowLitresPerHour);
        var avgSprinklerFlowLhr = demandLmin * 60 / effectiveCount;

        var mainPipe = design.Pipes.FirstOrDefault(p => p.IsMain);
        var mainDiameterMm = mainPipe is null ? i.MainDiameterMm : PipeDiameterMm(mainPipe);
        var mainVelocity = HydraulicsCalculators.VelocityMetresPerSecond(
            demandLmin * LitresPerMinuteToM3PerHour, mainDiameterMm);

        // Per-lateral hydraulics from the drawn layout: each placed sprinkler is
        // assigned to its nearest lateral, each lateral carries its own flow, and
        // lateral friction applies the Christiansen reduction (flow decreases at
        // every outlet). Results report the busiest lateral (worst path).
        var laterals = design.Pipes.Where(p => !p.IsMain).ToList();
        var lateralCountAssumed = laterals.Count == 0;

        // Connectivity: geometric assignment isn't plumbing, so at least flag
        // laterals that don't touch the main and sprinklers far from their lateral.
        var connectedLaterals = mainPipe is null
            ? new List<DesignPipe>()
            : laterals.Where(l => l.DistanceToPipe(mainPipe) <= LateralConnectToleranceM).ToList();
        if (laterals.Count > 0)
        {
            foreach (var lateral in laterals.Where(l => !connectedLaterals.Contains(l)))
            {
                disconnectedLateralIds.Add(lateral.Id);
            }

            if (mainPipe is null)
            {
                warnings.Add("No main pipe — lateral connections can't be checked.");
            }
            else if (connectedLaterals.Count == 0)
            {
                warnings.Add("None of the laterals connect to the main pipe — flows shown assume they do; check the layout.");
            }
            else if (disconnectedLateralIds.Count > 0)
            {
                warnings.Add($"{disconnectedLateralIds.Count} lateral(s) are not connected to the main pipe — they carry no flow.");
            }
        }

        double lateralLmin;
        double lateralVelocity;
        var lateralFrictionKpa = 0.0;
        var busiestSprinklers = 0;

        if (lateralCountAssumed)
        {
            // No laterals drawn: assumed even split, no drawn length → no lateral friction.
            lateralLmin = demandLmin / AssumedLateralCount;
            lateralVelocity = HydraulicsCalculators.VelocityMetresPerSecond(
                lateralLmin * LitresPerMinuteToM3PerHour, i.LateralDiameterMm);
        }
        else if (countFromCanvas)
        {
            // Sprinklers route only to laterals plumbed into the main; if none are
            // connected, fall back to nearest-any so results never vanish (warned above).
            var routable = connectedLaterals.Count > 0 ? connectedLaterals : laterals;

            var assignedCounts = laterals.ToDictionary(p => p.Id, _ => 0);
            var assignedFlowsLhr = laterals.ToDictionary(p => p.Id, _ => 0.0);
            foreach (var sprinkler in design.Sprinklers)
            {
                var nearest = routable.MinBy(p => p.DistanceToPoint(sprinkler.X, sprinkler.Y))!;
                assignedCounts[nearest.Id]++;
                assignedFlowsLhr[nearest.Id] += SprinklerFlowLhr(sprinkler);
                if (nearest.DistanceToPoint(sprinkler.X, sprinkler.Y) > SprinklerOnLateralToleranceM)
                {
                    farSprinklerIds.Add(sprinkler.Id);
                }
            }

            if (farSprinklerIds.Count > 0)
            {
                warnings.Add($"{farSprinklerIds.Count} sprinkler(s) are more than {SprinklerOnLateralToleranceM:0} m from their lateral.");
            }

            foreach (var lateral in laterals)
            {
                var assigned = assignedCounts[lateral.Id];
                var diameterMm = PipeDiameterMm(lateral);
                var flowLmin = assignedFlowsLhr[lateral.Id] / 60;
                var velocity = assigned > 0
                    ? HydraulicsCalculators.VelocityMetresPerSecond(flowLmin * LitresPerMinuteToM3PerHour, diameterMm)
                    : 0;
                var frictionForLateral = assigned > 0 && lateral.LengthMetres > 0
                    ? HydraulicsCalculators.LateralFrictionLoss(
                        flowLmin * LitresPerMinuteToM3PerHour, diameterMm,
                        lateral.LengthMetres, PipeRoughness.Upvc, assigned).PressureLossKpa
                    : 0;
                lateralStats[lateral.Id] = new LateralStats(assigned, flowLmin, velocity, frictionForLateral);
            }

            var busiest = lateralStats.Values.OrderByDescending(s => s.FlowLitresPerMinute).First();
            busiestSprinklers = busiest.SprinklerCount;
            lateralLmin = busiest.FlowLitresPerMinute;
            lateralVelocity = busiest.Velocity;
            lateralFrictionKpa = lateralStats.Values.Max(s => s.FrictionKpa);
        }
        else
        {
            // Laterals drawn but no sprinklers placed: even split of the input count,
            // Christiansen F from the per-lateral outlet estimate, on the longest lateral.
            lateralLmin = demandLmin / laterals.Count;
            var outletsPerLateral = Math.Max(1, (int)Math.Round((double)effectiveCount / laterals.Count));
            var longestLateral = laterals.MaxBy(p => p.LengthMetres)!;
            var longestDiameterMm = PipeDiameterMm(longestLateral);
            lateralVelocity = HydraulicsCalculators.VelocityMetresPerSecond(
                lateralLmin * LitresPerMinuteToM3PerHour, longestDiameterMm);

            if (longestLateral.LengthMetres > 0)
            {
                lateralFrictionKpa = HydraulicsCalculators.LateralFrictionLoss(
                    lateralLmin * LitresPerMinuteToM3PerHour, longestDiameterMm,
                    longestLateral.LengthMetres, PipeRoughness.Upvc, outletsPerLateral).PressureLossKpa;
            }

            foreach (var lateral in laterals)
            {
                lateralStats[lateral.Id] = new LateralStats(outletsPerLateral, lateralLmin, lateralVelocity, 0);
            }
        }

        var frictionKpa = lateralFrictionKpa;
        var mainLength = design.Pipes.Where(p => p.IsMain).Sum(p => p.LengthMetres);
        if (mainLength > 0)
        {
            frictionKpa += HydraulicsCalculators.HazenWilliamsFrictionLoss(
                demandLmin * LitresPerMinuteToM3PerHour, mainDiameterMm, mainLength, PipeRoughness.Upvc).PressureLossKpa;
        }

        var frictionBar = frictionKpa / 100;

        // Application rate/runtime/coverage follow the layout: average effective
        // sprinkler flow, and (with ≥2 heads placed) the mean nearest-neighbour
        // spacing of the placed heads — equal to the grid spacing on a uniform
        // grid. Clamped to the 1 m grid resolution so stacked heads can't zero it.
        var spacingFromCanvas = countFromCanvas && design.Sprinklers.Count >= 2;
        var effectiveSpacing = spacingFromCanvas
            ? Math.Max(1.0, ZoneDesignCalculators.MeanNearestNeighbourSpacing(
                design.Sprinklers.Select(s => (s.X, s.Y)).ToList()))
            : i.SprinklerSpacingMetres;

        var applicationRate = ApplicationCalculators.SprinklerRateMmPerHour(
            avgSprinklerFlowLhr, effectiveSpacing, effectiveSpacing);

        // The max-sprinklers solver uses the placed pump's rating when one exists.
        var pump = design.Points.FirstOrDefault(p => p.Kind == DesignPointKind.Pump);
        var pumpFlowLmin = pump?.RatedFlowLitresPerMinute ?? i.PumpFlowLitresPerMinute;

        var results = new DesignResults(
            effectiveCount,
            countFromCanvas,
            demandLmin,
            lateralLmin,
            lateralCountAssumed,
            busiestSprinklers,
            mainVelocity,
            lateralVelocity,
            frictionBar,
            frictionBar + SprinklerHeadPressureBar,
            ZoneDesignCalculators.CoverageRatio(
                countFromCanvas
                    ? design.Sprinklers.Average(s => s.RadiusMetres ?? i.SprinklerRadiusMetres)
                    : i.SprinklerRadiusMetres,
                effectiveSpacing),
            applicationRate,
            ZoneDesignCalculators.RunTimeMinutesForDepth(i.TargetDepthMm, applicationRate),
            ZoneDesignCalculators.MaxSprinklersForPumpFlow(pumpFlowLmin, i.SprinklerFlowLitresPerHour),
            avgSprinklerFlowLhr,
            pumpFlowLmin,
            effectiveSpacing,
            spacingFromCanvas);

        return new DesignEvaluation(results, lateralStats, warnings, disconnectedLateralIds, farSprinklerIds);
    }
}
