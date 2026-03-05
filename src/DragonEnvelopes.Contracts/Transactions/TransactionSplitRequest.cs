namespace DragonEnvelopes.Contracts.Transactions;

public sealed record TransactionSplitRequest(
    Guid EnvelopeId,
    decimal Amount,
    string? Category,
    string? Notes);

