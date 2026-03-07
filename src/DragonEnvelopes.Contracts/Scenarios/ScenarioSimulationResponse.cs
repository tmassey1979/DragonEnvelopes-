namespace DragonEnvelopes.Contracts.Scenarios;

public sealed record ScenarioSimulationResponse(
    Guid FamilyId,
    decimal StartingBalance,
    decimal MonthlyIncome,
    decimal FixedExpenses,
    decimal EffectiveExpenses,
    decimal NetMonthlyChange,
    int MonthHorizon,
    int? DepletionMonth,
    decimal EndingBalance,
    IReadOnlyList<ScenarioSimulationMonthResponse> Months);
