namespace DragonEnvelopes.Application.DTOs;

public sealed record EnvelopeRolloverPreviewDetails(
    Guid FamilyId,
    string Month,
    DateTimeOffset GeneratedAtUtc,
    decimal TotalSourceBalance,
    decimal TotalRolloverBalance,
    IReadOnlyList<EnvelopeRolloverItemDetails> Items);
