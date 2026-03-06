namespace DragonEnvelopes.Desktop.Services;

public sealed record BudgetMonthSummaryData(
    Guid Id,
    string Month,
    decimal TotalIncome,
    decimal AllocatedAmount,
    decimal RemainingAmount);
