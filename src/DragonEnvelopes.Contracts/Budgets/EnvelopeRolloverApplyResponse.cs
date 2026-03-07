namespace DragonEnvelopes.Contracts.Budgets;

public sealed record EnvelopeRolloverApplyResponse(
    Guid RunId,
    Guid FamilyId,
    string Month,
    bool AlreadyApplied,
    DateTimeOffset AppliedAtUtc,
    string? AppliedByUserId,
    int EnvelopeCount,
    decimal TotalRolloverBalance,
    IReadOnlyList<EnvelopeRolloverItemResponse> Items);
