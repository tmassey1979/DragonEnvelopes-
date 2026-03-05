namespace DragonEnvelopes.Contracts.Transactions;

public sealed record TransactionSplitResponse(
    Guid Id,
    Guid TransactionId,
    Guid EnvelopeId,
    decimal Amount,
    string? Category,
    string? Notes);

