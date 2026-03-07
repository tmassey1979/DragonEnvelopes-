namespace DragonEnvelopes.Contracts.Budgets;

public sealed record EnvelopeRolloverPreviewResponse(
    Guid FamilyId,
    string Month,
    DateTimeOffset GeneratedAtUtc,
    decimal TotalSourceBalance,
    decimal TotalRolloverBalance,
    IReadOnlyList<EnvelopeRolloverItemResponse> Items);
