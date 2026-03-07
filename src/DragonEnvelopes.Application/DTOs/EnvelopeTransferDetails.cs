namespace DragonEnvelopes.Application.DTOs;

public sealed record EnvelopeTransferDetails(
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
