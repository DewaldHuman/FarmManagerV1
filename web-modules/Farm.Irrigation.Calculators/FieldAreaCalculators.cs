namespace Farm.Irrigation.Calculators;

/// <summary>
/// Field area by shape (WSU Field Area calculator formulas), all in square metres.
/// Divide by 10 000 for hectares.
/// </summary>
public static class FieldAreaCalculators
{
    public static double RectangleSquareMetres(double lengthM, double widthM)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lengthM);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(widthM);
        return lengthM * widthM;
    }

    public static double TriangleSquareMetres(double baseM, double heightM)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseM);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(heightM);
        return baseM * heightM / 2;
    }

    /// <param name="portionFraction">Portion of the full circle, 0–1 (1 = full circle).</param>
    public static double CircleSquareMetres(double radiusM, double portionFraction = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radiusM);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(portionFraction);
        return Math.PI * radiusM * radiusM * portionFraction;
    }

    /// <summary>Trapezoid: two parallel sides and the perpendicular distance between them.</summary>
    public static double TrapezoidSquareMetres(double side1M, double side2M, double widthM)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(side1M);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(side2M);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(widthM);
        return (side1M + side2M) / 2 * widthM;
    }
}
