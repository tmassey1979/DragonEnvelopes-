namespace DragonEnvelopes.Application.DTOs;

public sealed record TransactionDetails(
    Guid Id,
    Guid AccountId,
    decimal Amount,
    string Description,
    string Merchant,
    DateTimeOffset OccurredAt,
    string? Category,
    Guid? EnvelopeId,
    IReadOnlyList<TransactionSplitDetails> Splits);

public sealed record TransactionSplitDetails(
    Guid EnvelopeId,
    decimal Amount,
    string? Category);
