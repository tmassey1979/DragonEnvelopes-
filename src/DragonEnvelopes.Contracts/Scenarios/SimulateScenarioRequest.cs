namespace DragonEnvelopes.Contracts.Scenarios;

public sealed record SimulateScenarioRequest(
    Guid FamilyId,
    decimal MonthlyIncome,
    decimal FixedExpenses,
    decimal? DiscretionaryCutPercent,
    int MonthHorizon);
