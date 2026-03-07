namespace DragonEnvelopes.Application.DTOs;

public sealed record FamilyFinancialProfileDetails(
    Guid Id,
    Guid FamilyId,
    bool PlaidConnected,
    string? PlaidItemId,
    bool StripeConnected,
    string? StripeCustomerId,
    DateTimeOffset? UpdatedAtUtc,
    decimal ReconciliationDriftThreshold = DragonEnvelopes.Domain.Entities.FamilyFinancialProfile.DefaultReconciliationDriftThreshold);

public sealed record PlaidLinkTokenDetails(
    string LinkToken,
    DateTimeOffset ExpiresAtUtc);

public sealed record StripeSetupIntentDetails(
    string CustomerId,
    string SetupIntentId,
    string ClientSecret);

public sealed record ProviderSecretsRewrapDetails(
    Guid FamilyId,
    bool ProfileFound,
    int FieldsTouched,
    DateTimeOffset ExecutedAtUtc);
