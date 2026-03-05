namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record BudgetAllocationEnvelopeViewModel(
    string Name,
    string MonthlyBudget,
    string CurrentBalance,
    string AllocationPercent,
    string BalancePercent,
    bool IsArchived);
