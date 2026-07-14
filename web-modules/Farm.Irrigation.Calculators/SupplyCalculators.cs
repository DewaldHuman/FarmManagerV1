namespace Farm.Irrigation.Calculators;

public readonly record struct DripTankResult(double TotalEmitters, double DailyDemandLitres, double TankLitres);

public readonly record struct LivestockWaterResult(double DailyLitres, double TankLitres);

public readonly record struct FieldTankResult(double GrossMmPerDay, double DailyCubicMetres, double TankKilolitres);

/// <summary>Water supply sizing — how much land a source can carry, how much storage is needed.</summary>
public static class SupplyCalculators
{
    /// <summary>
    /// Land area a water supply can irrigate at peak demand:
    /// daily supply ÷ gross daily need per hectare (net ÷ efficiency).
    /// </summary>
    public static double IrrigatableAreaHectares(
        double supplyFlowCubicMetresPerHour,
        double operatingHoursPerDay,
        double peakWaterUseMmPerDay,
        double efficiencyPercent)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(supplyFlowCubicMetresPerHour);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(operatingHoursPerDay);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(peakWaterUseMmPerDay);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(efficiencyPercent);

        var dailySupplyM3 = supplyFlowCubicMetresPerHour * operatingHoursPerDay;
        var grossNeedM3PerHa = peakWaterUseMmPerDay * 10 / (efficiencyPercent / 100.0);
        return dailySupplyM3 / grossNeedM3PerHa;
    }

    /// <summary>Storage capacity for a demand buffer: daily demand × reserve days × safety margin. 1 m³ = 1 kL.</summary>
    public static double StorageRequiredKilolitres(double dailyDemandCubicMetres, double reserveDays, double safetyMarginPercent)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dailyDemandCubicMetres);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(reserveDays);
        ArgumentOutOfRangeException.ThrowIfNegative(safetyMarginPercent);

        return dailyDemandCubicMetres * reserveDays * (1 + safetyMarginPercent / 100.0);
    }

    /// <summary>
    /// Drip system holding-tank size (watertankcalculator method):
    /// emitters = plants × emitters/plant; daily demand = emitters × flow × hours; tank = daily × buffer days.
    /// </summary>
    public static DripTankResult DripTankSize(
        double plantCount,
        double emittersPerPlant,
        double flowPerEmitterLitresPerHour,
        double hoursPerDay,
        double bufferDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(plantCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(emittersPerPlant);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flowPerEmitterLitresPerHour);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hoursPerDay);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferDays);

        var totalEmitters = plantCount * emittersPerPlant;
        var dailyLitres = totalEmitters * flowPerEmitterLitresPerHour * hoursPerDay;
        return new DripTankResult(totalEmitters, dailyLitres, dailyLitres * bufferDays);
    }

    /// <summary>
    /// Livestock drinking water: head count × daily rate × climate factor, buffered over reserve days.
    /// Rates in <see cref="LivestockWaterRates"/>; climate factor 1.0 temperate, up to 1.5 heat-stress.
    /// </summary>
    public static LivestockWaterResult LivestockWater(
        double headCount, double litresPerHeadPerDay, double climateFactor, double bufferDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(headCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(litresPerHeadPerDay);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(climateFactor);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferDays);

        var dailyLitres = headCount * litresPerHeadPerDay * climateFactor;
        return new LivestockWaterResult(dailyLitres, dailyLitres * bufferDays);
    }

    /// <summary>
    /// Greenhouse daily water use: area × crop rate × season factor ÷ irrigation efficiency.
    /// Crop rate is L/m²/day (1 L/m² = 1 mm depth).
    /// </summary>
    public static double GreenhouseDailyLitres(
        double areaSquareMetres,
        double cropRateLitresPerSquareMetreDay,
        double seasonFactor,
        double efficiencyPercent)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(areaSquareMetres);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cropRateLitresPerSquareMetreDay);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seasonFactor);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(efficiencyPercent);

        return areaSquareMetres * cropRateLitresPerSquareMetreDay * seasonFactor / (efficiencyPercent / 100.0);
    }

    /// <summary>
    /// Field irrigation tank sizing (watertankcalculator chain):
    /// gross = ETc ÷ efficiency; daily m³ = gross × ha × 10; tank = daily × refill interval × safety.
    /// </summary>
    public static FieldTankResult FieldIrrigationTankSize(
        double etcMmPerDay,
        double areaHectares,
        double efficiencyPercent,
        double refillIntervalDays,
        double safetyMarginPercent)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(etcMmPerDay);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(areaHectares);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(efficiencyPercent);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(refillIntervalDays);
        ArgumentOutOfRangeException.ThrowIfNegative(safetyMarginPercent);

        var grossMm = etcMmPerDay / (efficiencyPercent / 100.0);
        var dailyM3 = grossMm * areaHectares * 10;
        var tankKl = dailyM3 * refillIntervalDays * (1 + safetyMarginPercent / 100.0);
        return new FieldTankResult(grossMm, dailyM3, tankKl);
    }

    /// <summary>
    /// Required system capacity (WSU): flow to replace a net application depth over an area
    /// within an irrigation interval, running limited hours per day at a given efficiency.
    /// </summary>
    public static double RequiredSystemCapacityCubicMetresPerHour(
        double areaHectares,
        double netDepthPerIrrigationMm,
        double irrigationIntervalDays,
        double operatingHoursPerDay,
        double efficiencyPercent)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(areaHectares);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(netDepthPerIrrigationMm);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(irrigationIntervalDays);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(operatingHoursPerDay);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(efficiencyPercent);

        var grossVolumeM3 = areaHectares * netDepthPerIrrigationMm * 10 / (efficiencyPercent / 100.0);
        return grossVolumeM3 / (irrigationIntervalDays * operatingHoursPerDay);
    }
}
