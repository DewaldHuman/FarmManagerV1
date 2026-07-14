namespace Farm.Irrigation.Calculators;

public readonly record struct DripRunTime(double TotalFlowLitresPerHour, double RunTimeMinutes);

public readonly record struct Et0RunTimeResult(
    double CropWaterUseMmPerDay,
    double NetRequirementMm,
    double GrossRequirementMm,
    double VolumeLitres,
    double RunTimeMinutes);

public readonly record struct SetTimeResult(double Hours)
{
    public double Minutes => Hours * 60;
}

/// <summary>Calculators that answer "how long do I run the system?"</summary>
public static class RunTimeCalculators
{
    /// <summary>Run-time to deliver a target volume at a given flow rate.</summary>
    public static double ManualVolumeRunTimeMinutes(double targetVolumeLitres, double flowLitresPerMinute)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetVolumeLitres);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flowLitresPerMinute);
        return targetVolumeLitres / flowLitresPerMinute;
    }

    /// <summary>Run-time for a drip zone from emitter count and per-emitter flow.</summary>
    public static DripRunTime DripEmitterRunTime(double emitterCount, double flowPerEmitterLitresPerHour, double targetVolumeLitres)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(emitterCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flowPerEmitterLitresPerHour);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetVolumeLitres);

        var totalFlowLph = emitterCount * flowPerEmitterLitresPerHour;
        return new DripRunTime(totalFlowLph, targetVolumeLitres / totalFlowLph * 60);
    }

    /// <summary>
    /// FAO-style crop water requirement → run-time. ETc = ET₀ × Kc; net requirement is
    /// accumulated ETc since the last irrigation minus effective rainfall; gross divides
    /// by system efficiency; volume converts mm over the area (1 mm on 1 m² = 1 L).
    /// </summary>
    public static Et0RunTimeResult Et0RunTime(
        double et0MmPerDay,
        double cropCoefficient,
        double daysSinceLastIrrigation,
        double effectiveRainfallMm,
        double areaHectares,
        double systemFlowLitresPerMinute,
        double efficiencyPercent)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(et0MmPerDay);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cropCoefficient);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(daysSinceLastIrrigation);
        ArgumentOutOfRangeException.ThrowIfNegative(effectiveRainfallMm);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(areaHectares);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(systemFlowLitresPerMinute);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(efficiencyPercent);

        var etcMmPerDay = et0MmPerDay * cropCoefficient;
        var netMm = Math.Max(0, etcMmPerDay * daysSinceLastIrrigation - effectiveRainfallMm);
        var grossMm = netMm / (efficiencyPercent / 100.0);
        var volumeLitres = grossMm * areaHectares * 10_000; // mm × m² = L
        var runTimeMinutes = volumeLitres / systemFlowLitresPerMinute;

        return new Et0RunTimeResult(etcMmPerDay, netMm, grossMm, volumeLitres, runTimeMinutes);
    }

    /// <summary>Set time to apply a target depth over an area at a given flow rate.</summary>
    public static SetTimeResult SetTimeForDepth(double depthMm, double areaHectares, double flowCubicMetresPerHour)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(depthMm);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(areaHectares);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flowCubicMetresPerHour);

        // 1 mm over 1 ha = 10 m³
        var volumeCubicMetres = depthMm * areaHectares * 10;
        return new SetTimeResult(volumeCubicMetres / flowCubicMetresPerHour);
    }
}
