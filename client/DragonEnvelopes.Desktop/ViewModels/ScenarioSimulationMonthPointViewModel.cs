namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record ScenarioSimulationMonthPointViewModel(
    int MonthIndex,
    string Month,
    decimal IncomeValue,
    string Income,
    decimal ExpensesValue,
    string Expenses,
    decimal ProjectedBalanceValue,
    string ProjectedBalance,
    double ChartValue);
