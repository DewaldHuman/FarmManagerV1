namespace Farm.Irrigation.Calculators;

public readonly record struct IrrigationInterval(double ReadilyAvailableWaterMm, double IntervalDays);

/// <summary>Scheduling calculators — how often to irrigate.</summary>
public static class SchedulingCalculators
{
    /// <summary>
    /// Maximum interval between irrigations before the crop stresses:
    /// readily available water (soil AWC × root depth × allowable depletion) ÷ peak daily use.
    /// </summary>
    public static IrrigationInterval MaxIrrigationInterval(
        double availableWaterMmPerMetre,
        double rootDepthMetres,
        double allowableDepletionPercent,
        double peakWaterUseMmPerDay)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(availableWaterMmPerMetre);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rootDepthMetres);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(allowableDepletionPercent);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(peakWaterUseMmPerDay);

        var rawMm = availableWaterMmPerMetre * rootDepthMetres * (allowableDepletionPercent / 100.0);
        return new IrrigationInterval(rawMm, rawMm / peakWaterUseMmPerDay);
    }
}
