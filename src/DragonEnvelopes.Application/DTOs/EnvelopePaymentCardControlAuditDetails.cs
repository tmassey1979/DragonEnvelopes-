namespace DragonEnvelopes.Application.DTOs;

public sealed record EnvelopePaymentCardControlAuditDetails(
    Guid Id,
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId,
    string Action,
    string? PreviousStateJson,
    string NewStateJson,
    string ChangedBy,
    DateTimeOffset ChangedAtUtc);
