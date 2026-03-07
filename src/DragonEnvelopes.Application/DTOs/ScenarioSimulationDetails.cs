namespace DragonEnvelopes.Application.DTOs;

public sealed record ScenarioSimulationDetails(
    Guid FamilyId,
    decimal StartingBalance,
    decimal MonthlyIncome,
    decimal FixedExpenses,
    decimal EffectiveExpenses,
    decimal NetMonthlyChange,
    int MonthHorizon,
    int? DepletionMonth,
    decimal EndingBalance,
    IReadOnlyList<ScenarioSimulationMonthDetails> Months);

public sealed record ScenarioSimulationMonthDetails(
    int MonthIndex,
    string Month,
    decimal ProjectedBalance,
    decimal Income,
    decimal Expenses);
