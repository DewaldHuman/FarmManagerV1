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

    /// <summary>
    /// Effective sprinkler spacing of a placed layout: the mean, over all heads,
    /// of the distance to each head's nearest neighbour. Equals the grid spacing
    /// exactly on a uniform grid; a defensible "effective grid" figure otherwise.
    /// </summary>
    public static double MeanNearestNeighbourSpacing(IReadOnlyList<(double X, double Y)> points)
    {
        if (points.Count < 2)
        {
            throw new ArgumentException("At least two points are required.", nameof(points));
        }

        double total = 0;
        for (var i = 0; i < points.Count; i++)
        {
            var nearest = double.MaxValue;
            for (var j = 0; j < points.Count; j++)
            {
                if (j == i)
                {
                    continue;
                }

                var dx = points[i].X - points[j].X;
                var dy = points[i].Y - points[j].Y;
                nearest = Math.Min(nearest, Math.Sqrt(dx * dx + dy * dy));
            }

            total += nearest;
        }

        return total / points.Count;
    }
}
