using Xunit;

namespace Farm.Irrigation.Calculators.Tests;

public class RunTimeCalculatorTests
{
    [Fact]
    public void ManualVolume_MatchesDesignDemo_1200L_At35Lpm()
    {
        // Design reference demo: 1,200 L at 35 L/min → ~34 min
        var minutes = RunTimeCalculators.ManualVolumeRunTimeMinutes(1200, 35);
        Assert.Equal(34.29, minutes, 2);
    }

    [Fact]
    public void DripEmitter_MatchesDesignDemo_48Emitters_2Lph_80L()
    {
        // Design reference demo: 48 emitters at 2 L/hr, 80 L target → 50 min
        var result = RunTimeCalculators.DripEmitterRunTime(48, 2, 80);
        Assert.Equal(96, result.TotalFlowLitresPerHour);
        Assert.Equal(50, result.RunTimeMinutes, 2);
    }

    [Fact]
    public void Et0RunTime_MidSeason_NoRain()
    {
        // ET0 5.2 × Kc 1.05 = 5.46 mm/day; 3 days, no rain → 16.38 mm net;
        // 85% efficiency → 19.27 mm gross; 2.4 ha → 462,494 L; 600 L/min → 771 min
        var r = RunTimeCalculators.Et0RunTime(5.2, CropCoefficients.MidSeason, 3, 0, 2.4, 600, 85);
        Assert.Equal(5.46, r.CropWaterUseMmPerDay, 2);
        Assert.Equal(16.38, r.NetRequirementMm, 2);
        Assert.Equal(19.27, r.GrossRequirementMm, 2);
        Assert.Equal(462_494, r.VolumeLitres, 0);
        Assert.Equal(770.8, r.RunTimeMinutes, 1);
    }

    [Fact]
    public void Et0RunTime_RainfallCoversNeed_ClampsToZero()
    {
        var r = RunTimeCalculators.Et0RunTime(5.0, 1.0, 2, 50, 1, 100, 85);
        Assert.Equal(0, r.NetRequirementMm);
        Assert.Equal(0, r.RunTimeMinutes);
    }

    [Fact]
    public void SetTime_25mm_On1Ha_At60m3h()
    {
        // 25 mm on 1 ha = 250 m³ ÷ 60 m³/h = 4.1667 h
        var r = RunTimeCalculators.SetTimeForDepth(25, 1, 60);
        Assert.Equal(4.167, r.Hours, 3);
        Assert.Equal(250, r.Minutes, 0);
    }

    [Fact]
    public void ManualVolume_RejectsNonPositiveInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RunTimeCalculators.ManualVolumeRunTimeMinutes(0, 35));
        Assert.Throws<ArgumentOutOfRangeException>(() => RunTimeCalculators.ManualVolumeRunTimeMinutes(1200, 0));
    }
}

public class ApplicationCalculatorTests
{
    [Fact]
    public void ApplicationRate_60m3h_Over1Ha_Is6mmPerHour()
    {
        Assert.Equal(6, ApplicationCalculators.ApplicationRateMmPerHour(60, 1), 4);
    }

    [Fact]
    public void DepthApplied_60m3h_4Hours_1Ha_Is24mm()
    {
        Assert.Equal(24, ApplicationCalculators.DepthAppliedMm(60, 4, 1), 4);
    }

    [Fact]
    public void DripLineRate_2Lph_At_0_3x1_0m_Is6_67mmPerHour()
    {
        Assert.Equal(6.667, ApplicationCalculators.DripLineRateMmPerHour(2, 0.3, 1.0), 3);
    }

    [Fact]
    public void SprinklerRate_1000Lph_At12x12_Is6_94mmPerHour()
    {
        Assert.Equal(6.944, ApplicationCalculators.SprinklerRateMmPerHour(1000, 12, 12), 3);
    }
}

public class SchedulingCalculatorTests
{
    [Fact]
    public void MaxInterval_Loam_60cmRoots_50pct_6mmPerDay()
    {
        // 145 mm/m × 0.6 m × 50% = 43.5 mm RAW ÷ 6 mm/day = 7.25 days
        var r = SchedulingCalculators.MaxIrrigationInterval(SoilAvailableWater.Loam, 0.6, 50, 6);
        Assert.Equal(43.5, r.ReadilyAvailableWaterMm, 2);
        Assert.Equal(7.25, r.IntervalDays, 2);
    }
}

public class HydraulicsCalculatorTests
{
    [Fact]
    public void Velocity_60m3h_In100mmPipe()
    {
        // 0.016667 m³/s ÷ (π × 0.05²) = 2.122 m/s
        Assert.Equal(2.122, HydraulicsCalculators.VelocityMetresPerSecond(60, 100), 3);
    }

    [Fact]
    public void MinimumDiameter_60m3h_At1_5ms()
    {
        // d = √(4Q/(πv)) = 118.9 mm
        Assert.Equal(118.9, HydraulicsCalculators.MinimumDiameterMm(60, 1.5), 1);
    }

    [Fact]
    public void HazenWilliams_60m3h_100mm_200m_Upvc()
    {
        // hf = 10.67 × 200 × 0.016667^1.852 / (150^1.852 × 0.1^4.87) ≈ 7.5 m
        var r = HydraulicsCalculators.HazenWilliamsFrictionLoss(60, 100, 200, PipeRoughness.Upvc);
        Assert.InRange(r.HeadLossMetres, 7.2, 7.8);
        Assert.Equal(r.HeadLossMetres * 9.81, r.PressureLossKpa, 6);
    }

    [Fact]
    public void TotalDynamicHead_Lift_Friction_Pressure()
    {
        // 20 m + 7.5 m + 300 kPa (30.58 m) = 58.08 m
        Assert.Equal(58.08, HydraulicsCalculators.TotalDynamicHeadMetres(20, 7.5, 300), 2);
    }

    [Fact]
    public void PumpPower_60m3h_45m_65pct()
    {
        // hydraulic = 60 × 45 / 367 = 7.357 kW; shaft = / 0.65 = 11.32 kW
        var r = HydraulicsCalculators.PumpPowerKw(60, 45, 65);
        Assert.Equal(7.357, r.HydraulicKw, 3);
        Assert.Equal(11.32, r.ShaftKw, 2);
    }
}

public class SupplyCalculatorTests
{
    [Fact]
    public void IrrigatableArea_60m3h_10hPerDay_6mm_85pct()
    {
        // 600 m³/day ÷ (60 m³/ha ÷ 0.85 = 70.59 m³/ha) = 8.5 ha
        Assert.Equal(8.5, SupplyCalculators.IrrigatableAreaHectares(60, 10, 6, 85), 2);
    }

    [Fact]
    public void Storage_30m3PerDay_3Days_10pctMargin_Is99kL()
    {
        Assert.Equal(99, SupplyCalculators.StorageRequiredKilolitres(30, 3, 10), 4);
    }

    [Fact]
    public void DripTank_200Plants_2Emitters_2Lph_6h_3Days()
    {
        // watertankcalculator: emitters = 400; daily = 400×2×6 = 4,800 L; tank = ×3 = 14,400 L
        var r = SupplyCalculators.DripTankSize(200, 2, 2, 6, 3);
        Assert.Equal(400, r.TotalEmitters);
        Assert.Equal(4800, r.DailyDemandLitres, 4);
        Assert.Equal(14_400, r.TankLitres, 4);
    }

    [Fact]
    public void Livestock_100BeefCattle_HotDry_5DayBuffer()
    {
        // 100 × 45 L × 1.25 = 5,625 L/day; × 5 days = 28,125 L
        var r = SupplyCalculators.LivestockWater(100, LivestockWaterRates.BeefCattle, 1.25, 5);
        Assert.Equal(5625, r.DailyLitres, 4);
        Assert.Equal(28_125, r.TankLitres, 4);
    }

    [Fact]
    public void Greenhouse_300m2_Tomatoes_Summer_Drip()
    {
        // 300 × 4.5 × 1.25 ÷ 0.95 = 1,776 L/day
        Assert.Equal(1776.3, SupplyCalculators.GreenhouseDailyLitres(300, 4.5, 1.25, 95), 1);
    }

    [Fact]
    public void FieldTank_6mm_2ha_85pct_2DayRefill_10pctMargin()
    {
        // gross 7.06 mm; daily 141.2 m³; tank 141.2 × 2 × 1.1 = 310.6 kL
        var r = SupplyCalculators.FieldIrrigationTankSize(6, 2, 85, 2, 10);
        Assert.Equal(7.059, r.GrossMmPerDay, 3);
        Assert.Equal(141.18, r.DailyCubicMetres, 2);
        Assert.Equal(310.59, r.TankKilolitres, 2);
    }

    [Fact]
    public void SystemCapacity_8ha_30mm_7DayInterval_12hDays_85pct()
    {
        // WSU: Q = A×d/(interval×optime×eff) → 2,823.5 m³ ÷ 84 h = 33.6 m³/h
        Assert.Equal(33.61, SupplyCalculators.RequiredSystemCapacityCubicMetresPerHour(8, 30, 7, 12, 85), 2);
    }
}

public class NewApplicationCalculatorTests
{
    [Fact]
    public void SprinklerDensity_Rectangular_12x12_Is69PerHa()
    {
        // WSU rect: 43,560/(39.37² ft) = 28.1/acre = 69.4/ha
        Assert.Equal(69.44, ApplicationCalculators.SprinklerDensityRectangularPerHectare(12, 12), 2);
    }

    [Fact]
    public void SprinklerDensity_Triangular_12m_Is80PerHa()
    {
        // equilateral: rows at 12×√3/2 = 10.39 m → 80.2/ha
        Assert.Equal(80.19, ApplicationCalculators.SprinklerDensityTriangularPerHectare(12), 2);
    }

    [Fact]
    public void CatchCan_4_5_6_5mm_Over30min()
    {
        var r = ApplicationCalculators.CatchCanRate(4, 5, 6, 5, 30);
        Assert.Equal(5, r.AverageDepthMm, 4);
        Assert.Equal(10, r.RateMmPerHour, 4);
    }

    [Fact]
    public void SprinklerSession_12Heads_25Lpm_45min()
    {
        var r = ApplicationCalculators.SprinklerSessionVolume(12, 25, 45);
        Assert.Equal(300, r.TotalFlowLitresPerMinute, 4);
        Assert.Equal(13_500, r.VolumeLitres, 4);
    }
}

public class ChristiansenAndLateralTests
{
    [Fact]
    public void ChristiansenF_MatchesPublishedTable()
    {
        // Published Christiansen F values for m = 1.852
        Assert.Equal(1.0, HydraulicsCalculators.ChristiansenF(1), 4);
        Assert.Equal(0.402, HydraulicsCalculators.ChristiansenF(10), 3);
        Assert.Equal(0.376, HydraulicsCalculators.ChristiansenF(20), 3);
    }

    [Fact]
    public void LateralFrictionLoss_Is_FullPipeLoss_Times_F()
    {
        // full-pipe HW loss 7.52 m × F(20) 0.376 = 2.83 m ≈ 27.7 kPa
        var r = HydraulicsCalculators.LateralFrictionLoss(60, 100, 200, PipeRoughness.Upvc, 20);
        Assert.Equal(2.826, r.HeadLossMetres, 2);
        Assert.Equal(27.72, r.PressureLossKpa, 1);
    }
}

public class PumpSizingTests
{
    [Fact]
    public void PumpSizing_60m3h_45m_65_90pct_SF115_Recommends15kW()
    {
        // shaft 11.32 kW → input 12.58 kW → ×1.15 = 14.46 kW → next standard motor 15 kW
        var r = HydraulicsCalculators.PumpSizing(60, 45, 65, 90, 1.15);
        Assert.Equal(7.357, r.HydraulicKw, 3);
        Assert.Equal(11.32, r.ShaftKw, 2);
        Assert.Equal(12.58, r.InputKw, 2);
        Assert.Equal(15, r.RecommendedMotorKw);
    }
}

public class DemandCalculatorTests
{
    [Fact]
    public void CropWaterNeed_MidSeason_2_4ha_7Days()
    {
        // ETc = 5.2 × 1.05 = 5.46 mm/day; daily = 5.46 × 2.4 × 10 = 131.04 m³; ×7 = 917.28 m³
        var r = DemandCalculators.CropWaterNeed(5.2, CropCoefficients.MidSeason, 2.4, 7);
        Assert.Equal(5.46, r.EtcMmPerDay, 2);
        Assert.Equal(131.04, r.DailyVolumeCubicMetres, 2);
        Assert.Equal(917.28, r.TotalVolumeCubicMetres, 2);
    }

    [Fact]
    public void FarmWaterStorage_Livestock_Plus_Irrigation()
    {
        // livestock 100 × 45 = 4,500 L; irrigation 5 ha × 4 mm × 10,000 = 200,000 L;
        // total 204,500 L/day; tank ×3 = 613,500 L
        var r = DemandCalculators.FarmWaterStorage(100, LivestockWaterRates.BeefCattle, 5, 4, 3);
        Assert.Equal(4_500, r.LivestockDailyLitres, 4);
        Assert.Equal(200_000, r.IrrigationDailyLitres, 4);
        Assert.Equal(204_500, r.TotalDailyLitres, 4);
        Assert.Equal(613_500, r.TankLitres, 4);
    }

    [Fact]
    public void FarmWaterStorage_CropOnlyFarm_NoLivestock()
    {
        var r = DemandCalculators.FarmWaterStorage(0, LivestockWaterRates.BeefCattle, 5, 4, 3);
        Assert.Equal(0, r.LivestockDailyLitres);
        Assert.Equal(200_000, r.IrrigationDailyLitres, 4);
    }
}

public class FieldAreaCalculatorTests
{
    [Fact]
    public void Rectangle_120x85_Is10200m2()
    {
        Assert.Equal(10_200, FieldAreaCalculators.RectangleSquareMetres(120, 85), 4);
    }

    [Fact]
    public void Triangle_100Base_60Height_Is3000m2()
    {
        Assert.Equal(3000, FieldAreaCalculators.TriangleSquareMetres(100, 60), 4);
    }

    [Fact]
    public void Circle_Radius50_FullCircle_Is7854m2()
    {
        Assert.Equal(7853.98, FieldAreaCalculators.CircleSquareMetres(50), 2);
    }

    [Fact]
    public void Trapezoid_100_60Parallel_40Wide_Is3200m2()
    {
        Assert.Equal(3200, FieldAreaCalculators.TrapezoidSquareMetres(100, 60, 40), 4);
    }
}

public class ZoneDesignCalculatorTests
{
    [Fact]
    public void DemandFlow_15Sprinklers_200Lhr_Is50Lmin()
    {
        // 15 × 200 = 3,000 L/hr ÷ 60 = 50 L/min
        Assert.Equal(50, ZoneDesignCalculators.DemandFlowLitresPerMinute(15, 200), 4);
    }

    [Fact]
    public void MaxSprinklers_380LminPump_200LhrHeads_Is114()
    {
        // 380 × 60 = 22,800 L/hr ÷ 200 = 114
        Assert.Equal(114, ZoneDesignCalculators.MaxSprinklersForPumpFlow(380, 200));
    }

    [Fact]
    public void MaxSprinklers_RoundsDown()
    {
        // 10 × 60 = 600 ÷ 175 = 3.43 → 3
        Assert.Equal(3, ZoneDesignCalculators.MaxSprinklersForPumpFlow(10, 175));
    }

    [Fact]
    public void CoverageRatio_7mRadius_12mSpacing_Is1_17()
    {
        // 2 × 7 ÷ 12 = 1.1667
        Assert.Equal(1.1667, ZoneDesignCalculators.CoverageRatio(7, 12), 4);
    }

    [Fact]
    public void RunTime_12mm_At1_389mmPerHour_Is518min()
    {
        // Application rate 200 L/hr ÷ (12 × 12) m² = 1.3889 mm/hr;
        // 12 ÷ 1.3889 × 60 = 518.4 min (mockup shows 518)
        var rate = ApplicationCalculators.SprinklerRateMmPerHour(200, 12, 12);
        Assert.Equal(1.3889, rate, 4);
        Assert.Equal(518.4, ZoneDesignCalculators.RunTimeMinutesForDepth(12, rate), 1);
    }

    [Fact]
    public void MainVelocity_50Lmin_In50mmPipe_Is0_42ms()
    {
        // 50 L/min = 3 m³/h; 0.000833 m³/s ÷ (π × 0.025²) = 0.424 m/s
        Assert.Equal(0.424, HydraulicsCalculators.VelocityMetresPerSecond(50 * 0.06, 50), 3);
    }

    [Fact]
    public void LateralVelocity_ThirdOfDemand_In25mmPipe_Is0_57ms()
    {
        // 50 ÷ 3 = 16.67 L/min = 1 m³/h; 0.000278 m³/s ÷ (π × 0.0125²) = 0.566 m/s
        Assert.Equal(0.566, HydraulicsCalculators.VelocityMetresPerSecond(50.0 / 3 * 0.06, 25), 3);
    }

    [Fact]
    public void GuardClauses_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ZoneDesignCalculators.DemandFlowLitresPerMinute(0, 200));
        Assert.Throws<ArgumentOutOfRangeException>(() => ZoneDesignCalculators.MaxSprinklersForPumpFlow(380, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ZoneDesignCalculators.CoverageRatio(0, 12));
        Assert.Throws<ArgumentOutOfRangeException>(() => ZoneDesignCalculators.RunTimeMinutesForDepth(12, 0));
    }
}

public class MeanNearestNeighbourSpacingTests
{
    [Fact]
    public void UniformGrid_12mSpacing_14mRows_Is12()
    {
        // 5 columns at 12 m spacing × 3 rows 14 m apart: every head's nearest
        // neighbour is the 12 m one along its own row.
        var points = new List<(double X, double Y)>();
        foreach (var y in new[] { 0.0, 14.0, 28.0 })
        {
            for (var col = 0; col < 5; col++)
            {
                points.Add((col * 12.0, y));
            }
        }

        Assert.Equal(12.0, ZoneDesignCalculators.MeanNearestNeighbourSpacing(points), 4);
    }

    [Fact]
    public void TwoPoints_6mApart_Is6()
    {
        Assert.Equal(6.0, ZoneDesignCalculators.MeanNearestNeighbourSpacing(
            new List<(double X, double Y)> { (10, 10), (16, 10) }), 4);
    }

    [Fact]
    public void IrregularLShape_IsHandComputedMean()
    {
        // (0,0)→6, (6,0)→6, (6,8)→8; mean = 20/3 = 6.6667
        Assert.Equal(6.6667, ZoneDesignCalculators.MeanNearestNeighbourSpacing(
            new List<(double X, double Y)> { (0, 0), (6, 0), (6, 8) }), 4);
    }

    [Fact]
    public void FewerThanTwoPoints_Throws()
    {
        Assert.Throws<ArgumentException>(() => ZoneDesignCalculators.MeanNearestNeighbourSpacing(
            new List<(double X, double Y)> { (0, 0) }));
    }
}
