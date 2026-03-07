namespace DragonEnvelopes.Contracts.Transactions;

public sealed record TransactionResponse(
    Guid Id,
    Guid AccountId,
    decimal Amount,
    string Description,
    string Merchant,
    DateTimeOffset OccurredAt,
    string? Category,
    Guid? EnvelopeId,
    IReadOnlyList<TransactionSplitResponse> Splits,
    Guid? TransferId = null,
    Guid? TransferCounterpartyEnvelopeId = null,
    string? TransferDirection = null,
    DateTimeOffset? DeletedAtUtc = null,
    string? DeletedByUserId = null);
