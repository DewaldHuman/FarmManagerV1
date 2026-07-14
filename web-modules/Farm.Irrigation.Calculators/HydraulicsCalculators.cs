namespace Farm.Irrigation.Calculators;

public readonly record struct FrictionLoss(double HeadLossMetres, double PressureLossKpa);

public readonly record struct PumpPowerResult(double HydraulicKw, double ShaftKw);

public readonly record struct PumpSizingResult(
    double HydraulicKw,
    double ShaftKw,
    double InputKw,
    double RecommendedMotorKw);

/// <summary>Pipe and pump hydraulics (metric: m³/h, mm, m, kPa, kW).</summary>
public static class HydraulicsCalculators
{
    private const double KpaPerMetreHead = 9.81;

    /// <summary>Water velocity in a pipe from flow and inside diameter.</summary>
    public static double VelocityMetresPerSecond(double flowCubicMetresPerHour, double insideDiameterMm)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flowCubicMetresPerHour);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(insideDiameterMm);

        var flowM3s = flowCubicMetresPerHour / 3600;
        var diameterM = insideDiameterMm / 1000;
        var areaM2 = Math.PI * diameterM * diameterM / 4;
        return flowM3s / areaM2;
    }

    /// <summary>Minimum inside diameter to keep velocity at or below a target (SA guideline ~1.5 m/s).</summary>
    public static double MinimumDiameterMm(double flowCubicMetresPerHour, double targetVelocityMetresPerSecond)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flowCubicMetresPerHour);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetVelocityMetresPerSecond);

        var flowM3s = flowCubicMetresPerHour / 3600;
        var diameterM = Math.Sqrt(4 * flowM3s / (Math.PI * targetVelocityMetresPerSecond));
        return diameterM * 1000;
    }

    /// <summary>
    /// Hazen-Williams friction loss (metric form):
    /// hf = 10.67 · L · Q^1.852 / (C^1.852 · D^4.87), Q in m³/s, D in m.
    /// </summary>
    public static FrictionLoss HazenWilliamsFrictionLoss(
        double flowCubicMetresPerHour,
        double insideDiameterMm,
        double lengthMetres,
        double roughnessC)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flowCubicMetresPerHour);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(insideDiameterMm);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lengthMetres);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(roughnessC);

        var flowM3s = flowCubicMetresPerHour / 3600;
        var diameterM = insideDiameterMm / 1000;
        var headLossM = 10.67 * lengthMetres * Math.Pow(flowM3s, 1.852)
                        / (Math.Pow(roughnessC, 1.852) * Math.Pow(diameterM, 4.87));
        return new FrictionLoss(headLossM, headLossM * KpaPerMetreHead);
    }

    /// <summary>Total dynamic head from static lift, friction losses, and required outlet pressure.</summary>
    public static double TotalDynamicHeadMetres(double staticLiftMetres, double frictionLossMetres, double outletPressureKpa)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(staticLiftMetres);
        ArgumentOutOfRangeException.ThrowIfNegative(frictionLossMetres);
        ArgumentOutOfRangeException.ThrowIfNegative(outletPressureKpa);
        return staticLiftMetres + frictionLossMetres + outletPressureKpa / KpaPerMetreHead;
    }

    /// <summary>
    /// Pump power: hydraulic kW = Q·H / 367 (Q in m³/h, H in m);
    /// shaft kW divides by pump efficiency.
    /// </summary>
    public static PumpPowerResult PumpPowerKw(double flowCubicMetresPerHour, double totalDynamicHeadMetres, double pumpEfficiencyPercent)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flowCubicMetresPerHour);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalDynamicHeadMetres);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pumpEfficiencyPercent);

        var hydraulicKw = flowCubicMetresPerHour * totalDynamicHeadMetres / 367.0;
        return new PumpPowerResult(hydraulicKw, hydraulicKw / (pumpEfficiencyPercent / 100.0));
    }

    /// <summary>
    /// Full pump sizing (Hydraulic Institute method): shaft power from pump efficiency,
    /// electrical input power from motor efficiency, then the next standard IEC motor
    /// size at or above input × safety factor.
    /// </summary>
    public static PumpSizingResult PumpSizing(
        double flowCubicMetresPerHour,
        double totalDynamicHeadMetres,
        double pumpEfficiencyPercent,
        double motorEfficiencyPercent,
        double safetyFactor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(motorEfficiencyPercent);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(safetyFactor);

        var power = PumpPowerKw(flowCubicMetresPerHour, totalDynamicHeadMetres, pumpEfficiencyPercent);
        var inputKw = power.ShaftKw / (motorEfficiencyPercent / 100.0);
        var requiredKw = inputKw * safetyFactor;
        var motorKw = StandardMotors.SizesKw.FirstOrDefault(size => size >= requiredKw);
        if (motorKw == 0)
        {
            motorKw = requiredKw; // beyond the standard range — report the raw requirement
        }

        return new PumpSizingResult(power.HydraulicKw, power.ShaftKw, inputKw, motorKw);
    }

    /// <summary>
    /// Christiansen reduction coefficient F for a pipe with N equally spaced outlets:
    /// F = 1/(m+1) + 1/(2N) + √(m−1)/(6N²), with m = 1.852 (Hazen-Williams velocity
    /// exponent). F(1) is defined as 1 (all flow travels the full length).
    /// </summary>
    public static double ChristiansenF(int outletCount, double velocityExponent = 1.852)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(outletCount);
        if (outletCount == 1)
        {
            return 1.0;
        }

        var m = velocityExponent;
        double n = outletCount;
        return 1 / (m + 1) + 1 / (2 * n) + Math.Sqrt(m - 1) / (6 * n * n);
    }

    /// <summary>
    /// Friction loss in a lateral or manifold with equally spaced outlets
    /// (sprinkler lateral, drip line): Hazen-Williams loss × Christiansen F.
    /// Flow is the total flow entering the pipe.
    /// </summary>
    public static FrictionLoss LateralFrictionLoss(
        double flowCubicMetresPerHour,
        double insideDiameterMm,
        double lengthMetres,
        double roughnessC,
        int outletCount)
    {
        var fullPipe = HazenWilliamsFrictionLoss(flowCubicMetresPerHour, insideDiameterMm, lengthMetres, roughnessC);
        var f = ChristiansenF(outletCount);
        return new FrictionLoss(fullPipe.HeadLossMetres * f, fullPipe.PressureLossKpa * f);
    }
}
