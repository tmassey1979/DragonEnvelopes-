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
    string RolloverMode,
    decimal? RolloverCap,
    bool IsArchived);

public sealed record EnvelopeRolloverPreviewData(
    Guid FamilyId,
    string Month,
    DateTimeOffset GeneratedAtUtc,
    decimal TotalSourceBalance,
    decimal TotalRolloverBalance,
    IReadOnlyList<EnvelopeRolloverPreviewItemData> Items);

public sealed record EnvelopeRolloverApplyData(
    Guid RunId,
    Guid FamilyId,
    string Month,
    bool AlreadyApplied,
    DateTimeOffset AppliedAtUtc,
    int EnvelopeCount,
    decimal TotalRolloverBalance);

public sealed record EnvelopeRolloverPreviewItemData(
    Guid EnvelopeId,
    string EnvelopeName,
    decimal CurrentBalance,
    string RolloverMode,
    decimal? RolloverCap,
    decimal RolloverBalance,
    decimal AdjustmentAmount);
