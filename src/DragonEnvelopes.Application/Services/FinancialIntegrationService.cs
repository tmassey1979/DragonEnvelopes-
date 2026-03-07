using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Services;

public sealed class FinancialIntegrationService(
    IFamilyFinancialProfileRepository financialProfileRepository,
    IPlaidGateway plaidGateway,
    IStripeGateway stripeGateway,
    IClock clock) : IFinancialIntegrationService
{
    public async Task<FamilyFinancialProfileDetails> GetStatusAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        await EnsureFamilyExistsAsync(familyId, cancellationToken);

        var profile = await financialProfileRepository.GetByFamilyIdAsync(familyId, cancellationToken);
        return profile is null
            ? new FamilyFinancialProfileDetails(
                Guid.Empty,
                familyId,
                PlaidConnected: false,
                PlaidItemId: null,
                StripeConnected: false,
                StripeCustomerId: null,
                UpdatedAtUtc: null,
                ReconciliationDriftThreshold: FamilyFinancialProfile.DefaultReconciliationDriftThreshold)
            : Map(profile);
    }

    public async Task<PlaidLinkTokenDetails> CreatePlaidLinkTokenAsync(
        Guid familyId,
        string? clientUserId,
        string? clientName,
        CancellationToken cancellationToken = default)
    {
        await EnsureFamilyExistsAsync(familyId, cancellationToken);

        var resolvedClientUserId = string.IsNullOrWhiteSpace(clientUserId)
            ? familyId.ToString("N")
            : clientUserId.Trim();
        var resolvedClientName = string.IsNullOrWhiteSpace(clientName)
            ? "DragonEnvelopes"
            : clientName.Trim();

        var result = await plaidGateway.CreateLinkTokenAsync(
            familyId,
            resolvedClientUserId,
            resolvedClientName,
            cancellationToken);

        return new PlaidLinkTokenDetails(result.LinkToken, result.ExpiresAtUtc);
    }

    public async Task<FamilyFinancialProfileDetails> ExchangePlaidPublicTokenAsync(
        Guid familyId,
        string publicToken,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetOrCreateProfileForUpdateAsync(familyId, cancellationToken);
        var exchange = await plaidGateway.ExchangePublicTokenAsync(publicToken, cancellationToken);

        profile.SetPlaidConnection(exchange.ItemId, exchange.AccessToken, clock.UtcNow);
        await financialProfileRepository.SaveChangesAsync(cancellationToken);

        return Map(profile);
    }

    public async Task<StripeSetupIntentDetails> CreateStripeSetupIntentAsync(
        Guid familyId,
        string email,
        string? name,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetOrCreateProfileForUpdateAsync(familyId, cancellationToken);

        if (!profile.StripeConnected)
        {
            var customerId = await stripeGateway.CreateCustomerAsync(familyId, email, name, cancellationToken);
            profile.SetStripeCustomer(customerId, clock.UtcNow);
            await financialProfileRepository.SaveChangesAsync(cancellationToken);
        }

        var setupIntent = await stripeGateway.CreateSetupIntentAsync(
            profile.StripeCustomerId!,
            cancellationToken);

        return new StripeSetupIntentDetails(
            profile.StripeCustomerId!,
            setupIntent.SetupIntentId,
            setupIntent.ClientSecret);
    }

    public async Task<FamilyFinancialProfileDetails> UpdateReconciliationDriftThresholdAsync(
        Guid familyId,
        decimal reconciliationDriftThreshold,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetOrCreateProfileForUpdateAsync(familyId, cancellationToken);
        profile.SetReconciliationDriftThreshold(reconciliationDriftThreshold, clock.UtcNow);
        await financialProfileRepository.SaveChangesAsync(cancellationToken);
        return Map(profile);
    }

    public async Task<ProviderSecretsRewrapDetails> RewrapProviderSecretsAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        await EnsureFamilyExistsAsync(familyId, cancellationToken);

        var profile = await financialProfileRepository.GetByFamilyIdForUpdateAsync(familyId, cancellationToken);
        if (profile is null)
        {
            return new ProviderSecretsRewrapDetails(
                familyId,
                ProfileFound: false,
                FieldsTouched: 0,
                ExecutedAtUtc: clock.UtcNow);
        }

        var fieldsTouched = 0;
        if (!string.IsNullOrWhiteSpace(profile.PlaidAccessToken))
        {
            fieldsTouched += 1;
        }

        if (!string.IsNullOrWhiteSpace(profile.StripeCustomerId))
        {
            fieldsTouched += 1;
        }

        if (!string.IsNullOrWhiteSpace(profile.StripeDefaultPaymentMethodId))
        {
            fieldsTouched += 1;
        }

        await financialProfileRepository.SaveChangesAsync(cancellationToken);
        return new ProviderSecretsRewrapDetails(
            familyId,
            ProfileFound: true,
            FieldsTouched: fieldsTouched,
            ExecutedAtUtc: clock.UtcNow);
    }

    private async Task EnsureFamilyExistsAsync(Guid familyId, CancellationToken cancellationToken)
    {
        if (!await financialProfileRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }
    }

    private async Task<FamilyFinancialProfile> GetOrCreateProfileForUpdateAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyExistsAsync(familyId, cancellationToken);

        var profile = await financialProfileRepository.GetByFamilyIdForUpdateAsync(familyId, cancellationToken);
        if (profile is not null)
        {
            return profile;
        }

        var now = clock.UtcNow;
        profile = new FamilyFinancialProfile(
            Guid.NewGuid(),
            familyId,
            plaidItemId: null,
            plaidAccessToken: null,
            stripeCustomerId: null,
            stripeDefaultPaymentMethodId: null,
            createdAtUtc: now,
            updatedAtUtc: now);

        await financialProfileRepository.AddAsync(profile, cancellationToken);
        return profile;
    }

    private static FamilyFinancialProfileDetails Map(FamilyFinancialProfile profile)
    {
        return new FamilyFinancialProfileDetails(
            profile.Id,
            profile.FamilyId,
            profile.PlaidConnected,
            profile.PlaidItemId,
            profile.StripeConnected,
            profile.StripeCustomerId,
            profile.UpdatedAtUtc,
            profile.ReconciliationDriftThreshold);
    }
}
