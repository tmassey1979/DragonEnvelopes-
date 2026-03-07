using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IScenarioSimulationService
{
    Task<ScenarioSimulationDetails> SimulateAsync(
        Guid familyId,
        decimal monthlyIncome,
        decimal fixedExpenses,
        decimal? discretionaryCutPercent,
        int monthHorizon,
        decimal startingBalance,
        CancellationToken cancellationToken = default);
}
