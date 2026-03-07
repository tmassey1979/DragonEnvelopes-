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
    Guid? TransferId,
    Guid? TransferCounterpartyEnvelopeId,
    string? TransferDirection,
    IReadOnlyList<TransactionSplitDetails> Splits,
    DateTimeOffset? DeletedAtUtc = null,
    string? DeletedByUserId = null);

public sealed record TransactionSplitDetails(
    Guid Id,
    Guid TransactionId,
    Guid EnvelopeId,
    decimal Amount,
    string? Category,
    string? Notes);

public sealed record TransactionSplitCreateDetails(
    Guid EnvelopeId,
    decimal Amount,
    string? Category,
    string? Notes);
