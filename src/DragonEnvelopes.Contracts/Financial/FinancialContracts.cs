namespace DragonEnvelopes.Contracts.Financial;

public sealed record CreatePlaidLinkTokenRequest(
    string? ClientUserId,
    string? ClientName);

public sealed record CreatePlaidLinkTokenResponse(
    string LinkToken,
    DateTimeOffset ExpiresAtUtc);

public sealed record ExchangePlaidPublicTokenRequest(
    string PublicToken);

public sealed record CreateStripeSetupIntentRequest(
    string Email,
    string? Name);

public sealed record CreateStripeSetupIntentResponse(
    string CustomerId,
    string SetupIntentId,
    string ClientSecret);

public sealed record FamilyFinancialStatusResponse(
    Guid FamilyId,
    bool PlaidConnected,
    string? PlaidItemId,
    bool StripeConnected,
    string? StripeCustomerId,
    DateTimeOffset? UpdatedAtUtc);

public sealed record CreateStripeEnvelopeFinancialAccountRequest(
    string? DisplayName);

public sealed record EnvelopeFinancialAccountResponse(
    Guid Id,
    Guid FamilyId,
    Guid EnvelopeId,
    string Provider,
    string ProviderFinancialAccountId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateVirtualEnvelopeCardRequest(
    string? CardholderName);

public sealed record EnvelopePaymentCardResponse(
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

public sealed record UpsertEnvelopePaymentCardControlRequest(
    decimal? DailyLimitAmount,
    IReadOnlyList<string>? AllowedMerchantCategories,
    IReadOnlyList<string>? AllowedMerchantNames);

public sealed record EnvelopePaymentCardControlResponse(
    Guid Id,
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId,
    decimal? DailyLimitAmount,
    IReadOnlyList<string> AllowedMerchantCategories,
    IReadOnlyList<string> AllowedMerchantNames,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record EnvelopePaymentCardControlAuditResponse(
    Guid Id,
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId,
    string Action,
    string? PreviousStateJson,
    string NewStateJson,
    string ChangedBy,
    DateTimeOffset ChangedAtUtc);

public sealed record EvaluateEnvelopeCardSpendRequest(
    string MerchantName,
    string? MerchantCategory,
    decimal Amount,
    decimal SpentTodayAmount);

public sealed record EvaluateEnvelopeCardSpendResponse(
    bool IsAllowed,
    string? DenialReason,
    decimal? RemainingDailyLimit);
