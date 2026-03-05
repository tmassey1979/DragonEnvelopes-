namespace DragonEnvelopes.Contracts.Budgets;

public sealed record CreateBudgetRequest(
    Guid FamilyId,
    string Month,
    decimal TotalIncome);

