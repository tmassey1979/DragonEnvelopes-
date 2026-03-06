namespace DragonEnvelopes.Application.DTOs;

public sealed record PlaidAccountLinkDetails(
    Guid Id,
    Guid FamilyId,
    Guid AccountId,
    string PlaidAccountId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PlaidTransactionSyncDetails(
    Guid FamilyId,
    int PulledCount,
    int InsertedCount,
    int DedupedCount,
    int UnmappedCount,
    string? NextCursor,
    DateTimeOffset ProcessedAtUtc);
