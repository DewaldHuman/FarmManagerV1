namespace Farm.Irrigation.Calculators;

/// <summary>
/// Generic FAO-56 crop coefficients (Kc) per growth stage. Placeholder values until
/// Core Settings supplies per-crop coefficients (plan.md — Core: Settings).
/// </summary>
public static class CropCoefficients
{
    public const double Establishment = 0.50;
    public const double MidSeason = 1.05;
    public const double LateSeason = 0.80;
}

/// <summary>
/// Typical plant-available water holding capacity per soil texture, in mm of water
/// per metre of soil depth.
/// </summary>
public static class SoilAvailableWater
{
    public const double Sand = 60;
    public const double LoamySand = 85;
    public const double SandyLoam = 110;
    public const double Loam = 145;
    public const double ClayLoam = 160;
    public const double Clay = 150;
}

/// <summary>
/// Hazen-Williams roughness coefficients (C) for common irrigation pipe materials
/// (per irrigation.wsu.edu Pipeline Pressure Loss material list).
/// </summary>
public static class PipeRoughness
{
    public const double Upvc = 150;
    public const double Hdpe = 150;
    public const double AsbestosCement = 140;
    public const double NewSteel = 130;
    public const double GalvanizedSteel = 120;
    public const double AluminiumWithCouplers = 120;
    public const double OldSteel = 100;
}

/// <summary>
/// Average daily drinking-water consumption per head, litres/head/day
/// (FAO / USDA extension averages, per watertankcalculator.com livestock table).
/// </summary>
public static class LivestockWaterRates
{
    public const double BeefCattle = 45;
    public const double DairyCattle = 90;
    public const double Horses = 45;
    public const double Pigs = 15;
    public const double Sheep = 6;
    public const double Goats = 5;
    public const double PoultryPerBird = 0.5;
    public const double MixedOther = 25;
}

/// <summary>Standard IEC motor ratings (kW) commonly available in South Africa.</summary>
public static class StandardMotors
{
    public static readonly double[] SizesKw =
    {
        0.37, 0.55, 0.75, 1.1, 1.5, 2.2, 3, 4, 5.5, 7.5,
        11, 15, 18.5, 22, 30, 37, 45, 55, 75, 90, 110, 132,
    };
}
