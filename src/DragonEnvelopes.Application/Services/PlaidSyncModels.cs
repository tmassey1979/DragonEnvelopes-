namespace DragonEnvelopes.Application.Services;

public sealed record PlaidTransactionRecord(
    string PlaidTransactionId,
    string PlaidAccountId,
    decimal Amount,
    string Description,
    string Merchant,
    DateTimeOffset OccurredAtUtc);

public sealed record PlaidTransactionSyncResult(
    string? NextCursor,
    bool HasMore,
    IReadOnlyList<PlaidTransactionRecord> Added,
    IReadOnlyList<PlaidTransactionRecord> Modified);

public sealed record PlaidAccountBalanceRecord(
    string PlaidAccountId,
    decimal CurrentBalance);
