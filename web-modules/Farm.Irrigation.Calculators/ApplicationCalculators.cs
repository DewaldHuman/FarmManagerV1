namespace Farm.Irrigation.Calculators;

public readonly record struct CatchCanResult(double AverageDepthMm, double RateMmPerHour);

public readonly record struct SessionVolumeResult(double TotalFlowLitresPerMinute, double VolumeLitres);

/// <summary>Application-rate and depth-applied calculators (all outputs in mm / mm-per-hour).</summary>
public static class ApplicationCalculators
{
    /// <summary>Average application rate over an area: m³/h over ha → mm/h.</summary>
    public static double ApplicationRateMmPerHour(double flowCubicMetresPerHour, double areaHectares)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flowCubicMetresPerHour);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(areaHectares);
        return flowCubicMetresPerHour / (areaHectares * 10); // 10 m³ = 1 mm on 1 ha
    }

    /// <summary>Depth of water applied after running a known flow for a period over an area.</summary>
    public static double DepthAppliedMm(double flowCubicMetresPerHour, double runHours, double areaHectares)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flowCubicMetresPerHour);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runHours);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(areaHectares);
        return flowCubicMetresPerHour * runHours / (areaHectares * 10);
    }

    /// <summary>
    /// Drip line application rate from emitter flow and grid spacing.
    /// L/h over the m² each emitter wets = mm/h.
    /// </summary>
    public static double DripLineRateMmPerHour(double emitterFlowLitresPerHour, double emitterSpacingMetres, double lateralSpacingMetres)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(emitterFlowLitresPerHour);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(emitterSpacingMetres);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lateralSpacingMetres);
        return emitterFlowLitresPerHour / (emitterSpacingMetres * lateralSpacingMetres);
    }

    /// <summary>Sprinkler application rate from per-head flow and head spacing grid.</summary>
    public static double SprinklerRateMmPerHour(double sprinklerFlowLitresPerHour, double spacingAlongLateralMetres, double spacingBetweenLateralsMetres)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sprinklerFlowLitresPerHour);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(spacingAlongLateralMetres);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(spacingBetweenLateralsMetres);
        return sprinklerFlowLitresPerHour / (spacingAlongLateralMetres * spacingBetweenLateralsMetres);
    }

    /// <summary>Sprinkler heads per hectare on a rectangular grid.</summary>
    public static double SprinklerDensityRectangularPerHectare(double headSpacingMetres, double rowSpacingMetres)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(headSpacingMetres);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rowSpacingMetres);
        return 10_000 / (headSpacingMetres * rowSpacingMetres);
    }

    /// <summary>
    /// Sprinkler heads per hectare on an equilateral triangular (offset) pattern:
    /// row spacing works out to head spacing × √3/2.
    /// </summary>
    public static double SprinklerDensityTriangularPerHectare(double headSpacingMetres)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(headSpacingMetres);
        var rowSpacing = headSpacingMetres * Math.Sqrt(3) / 2;
        return 10_000 / (headSpacingMetres * rowSpacing);
    }

    /// <summary>
    /// Catch-can test (WSU Sprinkler Discharge method): average of four can depths
    /// over the operation time → measured application rate.
    /// </summary>
    public static CatchCanResult CatchCanRate(
        double can1Mm, double can2Mm, double can3Mm, double can4Mm, double operationMinutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(can1Mm);
        ArgumentOutOfRangeException.ThrowIfNegative(can2Mm);
        ArgumentOutOfRangeException.ThrowIfNegative(can3Mm);
        ArgumentOutOfRangeException.ThrowIfNegative(can4Mm);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(operationMinutes);

        var averageMm = (can1Mm + can2Mm + can3Mm + can4Mm) / 4;
        return new CatchCanResult(averageMm, averageMm / (operationMinutes / 60));
    }

    /// <summary>Water used by one sprinkler session: heads × per-head flow × run time.</summary>
    public static SessionVolumeResult SprinklerSessionVolume(
        double headCount, double flowPerHeadLitresPerMinute, double runMinutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(headCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flowPerHeadLitresPerMinute);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runMinutes);

        var totalFlowLpm = headCount * flowPerHeadLitresPerMinute;
        return new SessionVolumeResult(totalFlowLpm, totalFlowLpm * runMinutes);
    }
}
