namespace DragonEnvelopes.Desktop.Services;

public sealed record ScenarioSimulationWorkspaceData(
    Guid FamilyId,
    decimal StartingBalance,
    decimal MonthlyIncome,
    decimal FixedExpenses,
    decimal EffectiveExpenses,
    decimal NetMonthlyChange,
    int MonthHorizon,
    int? DepletionMonth,
    decimal EndingBalance,
    IReadOnlyList<ScenarioSimulationMonthData> Months);

public sealed record ScenarioSimulationMonthData(
    int MonthIndex,
    string Month,
    decimal Income,
    decimal Expenses,
    decimal ProjectedBalance);
