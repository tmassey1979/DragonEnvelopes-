using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IFinancialIntegrationService
{
    Task<FamilyFinancialProfileDetails> GetStatusAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<PlaidLinkTokenDetails> CreatePlaidLinkTokenAsync(
        Guid familyId,
        string? clientUserId,
        string? clientName,
        CancellationToken cancellationToken = default);

    Task<FamilyFinancialProfileDetails> ExchangePlaidPublicTokenAsync(
        Guid familyId,
        string publicToken,
        CancellationToken cancellationToken = default);

    Task<StripeSetupIntentDetails> CreateStripeSetupIntentAsync(
        Guid familyId,
        string email,
        string? name,
        CancellationToken cancellationToken = default);
}
