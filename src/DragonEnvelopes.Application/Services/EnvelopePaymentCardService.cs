using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Services;

public sealed class EnvelopePaymentCardService(
    IEnvelopeRepository envelopeRepository,
    IEnvelopeFinancialAccountRepository envelopeFinancialAccountRepository,
    IEnvelopePaymentCardRepository envelopePaymentCardRepository,
    IStripeGateway stripeGateway,
    IClock clock) : IEnvelopePaymentCardService
{
    private const string StripeProvider = "Stripe";
    private const string VirtualType = "Virtual";

    public async Task<EnvelopePaymentCardDetails> IssueVirtualCardAsync(
        Guid familyId,
        Guid envelopeId,
        string? cardholderName,
        CancellationToken cancellationToken = default)
    {
        var envelope = await envelopeRepository.GetByIdAsync(envelopeId, cancellationToken)
            ?? throw new DomainValidationException("Envelope was not found.");
        if (envelope.FamilyId != familyId)
        {
            throw new DomainValidationException("Envelope does not belong to the requested family.");
        }

        var linkedAccount = await envelopeFinancialAccountRepository.GetByEnvelopeIdAsync(envelopeId, cancellationToken)
            ?? throw new DomainValidationException("Envelope is not linked to a Stripe financial account.");
        if (!linkedAccount.Provider.Equals(StripeProvider, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("Envelope financial account provider is not supported for card issuing.");
        }

        var issueName = string.IsNullOrWhiteSpace(cardholderName)
            ? envelope.Name
            : cardholderName.Trim();

        var issued = await stripeGateway.CreateVirtualCardAsync(
            linkedAccount.ProviderFinancialAccountId,
            familyId,
            envelopeId,
            issueName,
            cancellationToken);

        var now = clock.UtcNow;
        var card = new EnvelopePaymentCard(
            Guid.NewGuid(),
            familyId,
            envelopeId,
            linkedAccount.Id,
            StripeProvider,
            issued.ProviderCardId,
            VirtualType,
            issued.Status,
            issued.Brand,
            issued.Last4,
            now,
            now);

        await envelopePaymentCardRepository.AddAsync(card, cancellationToken);
        return Map(card);
    }

    public async Task<IReadOnlyList<EnvelopePaymentCardDetails>> ListByEnvelopeAsync(
        Guid familyId,
        Guid envelopeId,
        CancellationToken cancellationToken = default)
    {
        var envelope = await envelopeRepository.GetByIdAsync(envelopeId, cancellationToken)
            ?? throw new DomainValidationException("Envelope was not found.");
        if (envelope.FamilyId != familyId)
        {
            throw new DomainValidationException("Envelope does not belong to the requested family.");
        }

        var cards = await envelopePaymentCardRepository.ListByEnvelopeAsync(envelopeId, cancellationToken);
        return cards.Select(Map).ToArray();
    }

    public Task<EnvelopePaymentCardDetails> FreezeCardAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        return UpdateCardStatusAsync(familyId, envelopeId, cardId, "Inactive", cancellationToken);
    }

    public Task<EnvelopePaymentCardDetails> UnfreezeCardAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        return UpdateCardStatusAsync(familyId, envelopeId, cardId, "Active", cancellationToken);
    }

    public Task<EnvelopePaymentCardDetails> CancelCardAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        return UpdateCardStatusAsync(familyId, envelopeId, cardId, "Canceled", cancellationToken);
    }

    private async Task<EnvelopePaymentCardDetails> UpdateCardStatusAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        string status,
        CancellationToken cancellationToken)
    {
        var card = await envelopePaymentCardRepository.GetByIdForUpdateAsync(cardId, cancellationToken)
            ?? throw new DomainValidationException("Card was not found.");
        if (card.FamilyId != familyId || card.EnvelopeId != envelopeId)
        {
            throw new DomainValidationException("Card does not belong to the requested family/envelope.");
        }

        await stripeGateway.UpdateCardStatusAsync(card.ProviderCardId, status, cancellationToken);
        card.ChangeStatus(status, clock.UtcNow);
        await envelopePaymentCardRepository.SaveChangesAsync(cancellationToken);

        return Map(card);
    }

    private static EnvelopePaymentCardDetails Map(EnvelopePaymentCard card)
    {
        return new EnvelopePaymentCardDetails(
            card.Id,
            card.FamilyId,
            card.EnvelopeId,
            card.EnvelopeFinancialAccountId,
            card.Provider,
            card.ProviderCardId,
            card.Type,
            card.Status,
            card.Brand,
            card.Last4,
            card.CreatedAtUtc,
            card.UpdatedAtUtc);
    }
}
