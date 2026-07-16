namespace Farm.Irrigation.Calculators;

/// <summary>
/// Zone Design layout calculators (metric: L/min, L/h, m, mm).
/// Complements HydraulicsCalculators/ApplicationCalculators for the designer's
/// sprinkler-grid model; pipe velocity, friction and application rate reuse
/// those classes directly (converting L/min → m³/h at the call site).
/// </summary>
public static class ZoneDesignCalculators
{
    /// <summary>Total demand flow of all sprinklers: count × per-head L/h → L/min.</summary>
    public static double DemandFlowLitresPerMinute(int sprinklerCount, double flowPerSprinklerLitresPerHour)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sprinklerCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flowPerSprinklerLitresPerHour);
        return sprinklerCount * flowPerSprinklerLitresPerHour / 60;
    }

    /// <summary>Largest whole number of sprinklers a pump's rated flow can supply.</summary>
    public static int MaxSprinklersForPumpFlow(double pumpFlowLitresPerMinute, double flowPerSprinklerLitresPerHour)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pumpFlowLitresPerMinute);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flowPerSprinklerLitresPerHour);
        return (int)Math.Floor(pumpFlowLitresPerMinute * 60 / flowPerSprinklerLitresPerHour);
    }

    /// <summary>
    /// Sprinkler coverage ratio: wetted diameter over head spacing (2r / s).
    /// Around 1.0 the pattern just meets; below ~0.9 there are dry gaps,
    /// above ~1.3 heads overlap heavily.
    /// </summary>
    public static double CoverageRatio(double sprinklerRadiusMetres, double spacingMetres)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sprinklerRadiusMetres);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(spacingMetres);
        return 2 * sprinklerRadiusMetres / spacingMetres;
    }

    /// <summary>Minutes to apply a target depth at a known application rate.</summary>
    public static double RunTimeMinutesForDepth(double targetDepthMm, double applicationRateMmPerHour)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetDepthMm);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(applicationRateMmPerHour);
        return targetDepthMm / applicationRateMmPerHour * 60;
    }
}
