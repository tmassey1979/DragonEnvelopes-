namespace DragonEnvelopes.Contracts.Budgets;

public sealed record BudgetResponse(
    Guid Id,
    Guid FamilyId,
    string Month,
    decimal TotalIncome,
    decimal AllocatedAmount,
    decimal RemainingAmount);

