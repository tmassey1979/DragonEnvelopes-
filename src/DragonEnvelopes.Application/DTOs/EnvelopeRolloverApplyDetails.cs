namespace DragonEnvelopes.Application.DTOs;

public sealed record EnvelopeRolloverApplyDetails(
    Guid RunId,
    Guid FamilyId,
    string Month,
    bool AlreadyApplied,
    DateTimeOffset AppliedAtUtc,
    string? AppliedByUserId,
    int EnvelopeCount,
    decimal TotalRolloverBalance,
    IReadOnlyList<EnvelopeRolloverItemDetails> Items);
