namespace DragonEnvelopes.Application.DTOs;

public sealed record RemainingBudgetDetails(
    decimal TotalIncome,
    decimal AllocatedAmount,
    decimal RemainingAmount);
