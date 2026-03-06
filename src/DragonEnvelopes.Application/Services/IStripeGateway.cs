namespace DragonEnvelopes.Application.Services;

public interface IStripeGateway
{
    Task<string> CreateCustomerAsync(
        Guid familyId,
        string email,
        string? name,
        CancellationToken cancellationToken = default);

    Task<(string SetupIntentId, string ClientSecret)> CreateSetupIntentAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    Task<string> CreateFinancialAccountAsync(
        string customerId,
        Guid familyId,
        Guid envelopeId,
        string displayName,
        CancellationToken cancellationToken = default);

    Task<(string ProviderCardId, string Status, string? Brand, string? Last4)> CreateVirtualCardAsync(
        string financialAccountId,
        Guid familyId,
        Guid envelopeId,
        string cardholderName,
        CancellationToken cancellationToken = default);

    Task<StripeCardIssuanceResult> CreatePhysicalCardAsync(
        string financialAccountId,
        Guid familyId,
        Guid envelopeId,
        string cardholderName,
        StripeCardShippingAddress shippingAddress,
        CancellationToken cancellationToken = default);

    Task UpdateCardStatusAsync(
        string providerCardId,
        string status,
        CancellationToken cancellationToken = default);

    Task UpdateCardSpendingControlsAsync(
        string providerCardId,
        decimal? dailyLimitAmount,
        IReadOnlyList<string> allowedMerchantCategories,
        IReadOnlyList<string> allowedMerchantNames,
        CancellationToken cancellationToken = default);

    Task<StripeCardStatusResult> GetCardStatusAsync(
        string providerCardId,
        CancellationToken cancellationToken = default);
}
