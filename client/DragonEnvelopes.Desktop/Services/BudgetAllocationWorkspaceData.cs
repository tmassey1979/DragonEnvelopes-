namespace DragonEnvelopes.Desktop.Services;

public sealed record BudgetAllocationWorkspaceData(
    string Month,
    decimal TotalIncome,
    decimal AllocatedAmount,
    decimal RemainingAmount,
    IReadOnlyList<BudgetAllocationEnvelopeData> Envelopes);

public sealed record BudgetAllocationEnvelopeData(
    Guid EnvelopeId,
    string EnvelopeName,
    decimal MonthlyBudget,
    decimal CurrentBalance,
    bool IsArchived);
