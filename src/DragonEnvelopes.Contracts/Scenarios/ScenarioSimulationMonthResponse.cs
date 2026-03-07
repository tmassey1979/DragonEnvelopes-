namespace DragonEnvelopes.Contracts.Scenarios;

public sealed record ScenarioSimulationMonthResponse(
    int MonthIndex,
    string Month,
    decimal ProjectedBalance,
    decimal Income,
    decimal Expenses);
