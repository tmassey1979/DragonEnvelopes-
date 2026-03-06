using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Services;

public sealed class EnvelopeFinancialAccountService(
    IEnvelopeRepository envelopeRepository,
    IEnvelopeFinancialAccountRepository envelopeFinancialAccountRepository,
    IFamilyFinancialProfileRepository familyFinancialProfileRepository,
    IStripeGateway stripeGateway,
    IClock clock) : IEnvelopeFinancialAccountService
{
    private const string StripeProvider = "Stripe";

    public async Task<EnvelopeFinancialAccountDetails> LinkStripeFinancialAccountAsync(
        Guid familyId,
        Guid envelopeId,
        string? displayName,
        CancellationToken cancellationToken = default)
    {
        var envelope = await envelopeRepository.GetByIdAsync(envelopeId, cancellationToken)
            ?? throw new DomainValidationException("Envelope was not found.");
        if (envelope.FamilyId != familyId)
        {
            throw new DomainValidationException("Envelope does not belong to the requested family.");
        }

        var profile = await familyFinancialProfileRepository.GetByFamilyIdForUpdateAsync(familyId, cancellationToken)
            ?? throw new DomainValidationException("Stripe customer is not connected for this family.");
        if (!profile.StripeConnected || string.IsNullOrWhiteSpace(profile.StripeCustomerId))
        {
            throw new DomainValidationException("Stripe customer is not connected for this family.");
        }

        var existing = await envelopeFinancialAccountRepository.GetByEnvelopeIdForUpdateAsync(envelopeId, cancellationToken);
        if (existing is not null && existing.Provider.Equals(StripeProvider, StringComparison.OrdinalIgnoreCase))
        {
            return Map(existing);
        }

        var accountDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? envelope.Name
            : displayName.Trim();

        var stripeFinancialAccountId = await stripeGateway.CreateFinancialAccountAsync(
            profile.StripeCustomerId,
            familyId,
            envelopeId,
            accountDisplayName,
            cancellationToken);

        if (existing is null)
        {
            var now = clock.UtcNow;
            var linked = new EnvelopeFinancialAccount(
                Guid.NewGuid(),
                familyId,
                envelopeId,
                StripeProvider,
                stripeFinancialAccountId,
                now,
                now);
            await envelopeFinancialAccountRepository.AddAsync(linked, cancellationToken);
            return Map(linked);
        }

        existing.Rebind(StripeProvider, stripeFinancialAccountId, clock.UtcNow);
        await envelopeFinancialAccountRepository.SaveChangesAsync(cancellationToken);
        return Map(existing);
    }

    public async Task<EnvelopeFinancialAccountDetails?> GetByEnvelopeAsync(
        Guid familyId,
        Guid envelopeId,
        CancellationToken cancellationToken = default)
    {
        var account = await envelopeFinancialAccountRepository.GetByEnvelopeIdAsync(envelopeId, cancellationToken);
        if (account is null || account.FamilyId != familyId)
        {
            return null;
        }

        return Map(account);
    }

    public async Task<IReadOnlyList<EnvelopeFinancialAccountDetails>> ListByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        if (!await envelopeRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        var accounts = await envelopeFinancialAccountRepository.ListByFamilyAsync(familyId, cancellationToken);
        return accounts.Select(Map).ToArray();
    }

    private static EnvelopeFinancialAccountDetails Map(EnvelopeFinancialAccount account)
    {
        return new EnvelopeFinancialAccountDetails(
            account.Id,
            account.FamilyId,
            account.EnvelopeId,
            account.Provider,
            account.ProviderFinancialAccountId,
            account.CreatedAtUtc,
            account.UpdatedAtUtc);
    }
}
