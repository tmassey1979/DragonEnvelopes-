namespace DragonEnvelopes.Application.DTOs;

public sealed record FamilyFinancialProfileDetails(
    Guid Id,
    Guid FamilyId,
    bool PlaidConnected,
    string? PlaidItemId,
    bool StripeConnected,
    string? StripeCustomerId,
    DateTimeOffset? UpdatedAtUtc);

public sealed record PlaidLinkTokenDetails(
    string LinkToken,
    DateTimeOffset ExpiresAtUtc);

public sealed record StripeSetupIntentDetails(
    string CustomerId,
    string SetupIntentId,
    string ClientSecret);
