namespace DragonEnvelopes.Contracts.Transactions;

public sealed record UpdateTransactionRequest(
    string Description,
    string Merchant,
    string? Category,
    bool ReplaceAllocation,
    Guid? EnvelopeId,
    IReadOnlyList<TransactionSplitRequest>? Splits);
