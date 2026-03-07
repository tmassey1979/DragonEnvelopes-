namespace DragonEnvelopes.Contracts.Transactions;

public sealed record CreateEnvelopeTransferRequest(
    Guid FamilyId,
    Guid AccountId,
    Guid FromEnvelopeId,
    Guid ToEnvelopeId,
    decimal Amount,
    DateTimeOffset OccurredAt,
    string? Notes);
