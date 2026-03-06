namespace DragonEnvelopes.Application.DTOs;

public sealed record EnvelopeFinancialAccountDetails(
    Guid Id,
    Guid FamilyId,
    Guid EnvelopeId,
    string Provider,
    string ProviderFinancialAccountId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
