namespace DragonEnvelopes.Contracts.Transactions;

public sealed record CreateTransactionRequest(
    Guid AccountId,
    decimal Amount,
    string Description,
    string Merchant,
    DateTimeOffset OccurredAt,
    string? Category,
    Guid? EnvelopeId,
    IReadOnlyList<TransactionSplitRequest>? Splits);

