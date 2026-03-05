namespace DragonEnvelopes.Application.DTOs;

public sealed record BudgetDetails(
    Guid Id,
    Guid FamilyId,
    string Month,
    decimal TotalIncome,
    decimal AllocatedAmount,
    decimal RemainingAmount);
