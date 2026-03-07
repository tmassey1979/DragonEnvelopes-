namespace DragonEnvelopes.Desktop.Services;

public interface IScenarioSimulationDataService
{
    Task<ScenarioSimulationWorkspaceData> SimulateAsync(
        decimal monthlyIncome,
        decimal fixedExpenses,
        decimal? discretionaryCutPercent,
        int monthHorizon,
        CancellationToken cancellationToken = default);
}
