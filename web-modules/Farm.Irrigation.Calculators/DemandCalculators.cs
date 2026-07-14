namespace Farm.Irrigation.Calculators;

public readonly record struct CropWaterNeedResult(
    double EtcMmPerDay,
    double DailyVolumeCubicMetres,
    double TotalVolumeCubicMetres);

public readonly record struct FarmStorageResult(
    double LivestockDailyLitres,
    double IrrigationDailyLitres,
    double TotalDailyLitres,
    double TankLitres);

/// <summary>Whole-farm water demand (crop need, combined farm storage).</summary>
public static class DemandCalculators
{
    /// <summary>
    /// FAO-56 crop water need: ETc = Kc × ET₀, then volume over the growing period.
    /// 1 mm of depth over 1 ha = 10 m³ (agronomic demand, before any system losses).
    /// </summary>
    public static CropWaterNeedResult CropWaterNeed(
        double et0MmPerDay, double cropCoefficient, double areaHectares, double days)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(et0MmPerDay);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cropCoefficient);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(areaHectares);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);

        var etc = et0MmPerDay * cropCoefficient;
        var dailyM3 = etc * areaHectares * 10;
        return new CropWaterNeedResult(etc, dailyM3, dailyM3 * days);
    }

    /// <summary>
    /// Farm water storage (watertankcalculator method): combined daily demand from
    /// livestock and irrigation, buffered over a reserve period.
    /// Livestock = head × L/head/day. Irrigation = area(ha) × mm/day × 10,000
    /// (1 mm over 1 ha = 10,000 L). Tank = total daily × reserve days.
    /// Either component may be zero (crop-only or livestock-only farm).
    /// </summary>
    public static FarmStorageResult FarmWaterStorage(
        double livestockHead,
        double litresPerHeadPerDay,
        double irrigationAreaHectares,
        double cropUseMmPerDay,
        double reserveDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(livestockHead);
        ArgumentOutOfRangeException.ThrowIfNegative(litresPerHeadPerDay);
        ArgumentOutOfRangeException.ThrowIfNegative(irrigationAreaHectares);
        ArgumentOutOfRangeException.ThrowIfNegative(cropUseMmPerDay);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(reserveDays);

        var livestock = livestockHead * litresPerHeadPerDay;
        var irrigation = irrigationAreaHectares * cropUseMmPerDay * 10_000;
        var total = livestock + irrigation;
        return new FarmStorageResult(livestock, irrigation, total, total * reserveDays);
    }
}
