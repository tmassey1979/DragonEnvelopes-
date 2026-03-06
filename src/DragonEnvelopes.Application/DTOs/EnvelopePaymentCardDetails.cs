namespace DragonEnvelopes.Application.DTOs;

public sealed record EnvelopePaymentCardDetails(
    Guid Id,
    Guid FamilyId,
    Guid EnvelopeId,
    Guid EnvelopeFinancialAccountId,
    string Provider,
    string ProviderCardId,
    string Type,
    string Status,
    string? Brand,
    string? Last4,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
