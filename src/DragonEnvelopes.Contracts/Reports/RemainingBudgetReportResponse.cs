namespace DragonEnvelopes.Contracts.Reports;

public sealed record RemainingBudgetReportResponse(
    Guid BudgetId,
    Guid FamilyId,
    string Month,
    decimal TotalIncome,
    decimal AllocatedAmount,
    decimal RemainingAmount);
