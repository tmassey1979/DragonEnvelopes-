namespace DragonEnvelopes.Contracts.Transactions;

public sealed record EnvelopeTransferResponse(
    Guid TransferId,
    Guid FamilyId,
    Guid AccountId,
    Guid FromEnvelopeId,
    Guid ToEnvelopeId,
    decimal Amount,
    DateTimeOffset OccurredAt,
    string? Notes,
    Guid DebitTransactionId,
    Guid CreditTransactionId);
