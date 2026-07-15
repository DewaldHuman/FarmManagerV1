namespace Farm.Web.Core.Irrigation;

public interface IIrrigationService
{
    Task<CalculationRunDto?> LogCalculationRunAsync(
        Guid zoneId, string calculatorType, Dictionary<string, double> inputs, Dictionary<string, double> outputs);

    Task<List<CalculationRunDto>> ListCalculationRunsAsync(Guid zoneId);
}
