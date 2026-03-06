using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Services;

public sealed class EnvelopePaymentCardService(
    IEnvelopeRepository envelopeRepository,
    IEnvelopeFinancialAccountRepository envelopeFinancialAccountRepository,
    IEnvelopePaymentCardRepository envelopePaymentCardRepository,
    IEnvelopePaymentCardShipmentRepository envelopePaymentCardShipmentRepository,
    IStripeGateway stripeGateway,
    IClock clock) : IEnvelopePaymentCardService
{
    private const string StripeProvider = "Stripe";
    private const string VirtualType = "Virtual";
    private const string PhysicalType = "Physical";

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

    public async Task<EnvelopePhysicalCardIssuanceDetails> IssuePhysicalCardAsync(
        Guid familyId,
        Guid envelopeId,
        string? cardholderName,
        string recipientName,
        string addressLine1,
        string? addressLine2,
        string city,
        string stateOrProvince,
        string postalCode,
        string countryCode,
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
        var shippingAddress = new StripeCardShippingAddress(
            recipientName.Trim(),
            addressLine1.Trim(),
            string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim(),
            city.Trim(),
            stateOrProvince.Trim(),
            postalCode.Trim(),
            countryCode.Trim().ToUpperInvariant());

        var issued = await stripeGateway.CreatePhysicalCardAsync(
            linkedAccount.ProviderFinancialAccountId,
            familyId,
            envelopeId,
            issueName,
            shippingAddress,
            cancellationToken);

        var now = clock.UtcNow;
        var card = new EnvelopePaymentCard(
            Guid.NewGuid(),
            familyId,
            envelopeId,
            linkedAccount.Id,
            StripeProvider,
            issued.ProviderCardId,
            PhysicalType,
            issued.Status,
            issued.Brand,
            issued.Last4,
            now,
            now);
        await envelopePaymentCardRepository.AddAsync(card, cancellationToken);

        var shipment = new EnvelopePaymentCardShipment(
            Guid.NewGuid(),
            familyId,
            envelopeId,
            card.Id,
            shippingAddress.RecipientName,
            shippingAddress.Line1,
            shippingAddress.Line2,
            shippingAddress.City,
            shippingAddress.State,
            shippingAddress.PostalCode,
            shippingAddress.CountryCode,
            issued.ShipmentStatus,
            issued.ShipmentCarrier,
            issued.ShipmentTrackingNumber,
            now,
            now);
        await envelopePaymentCardShipmentRepository.AddAsync(shipment, cancellationToken);

        return new EnvelopePhysicalCardIssuanceDetails(
            Map(card),
            Map(shipment));
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

    public async Task<EnvelopePhysicalCardIssuanceDetails?> GetPhysicalCardIssuanceAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var card = await envelopePaymentCardRepository.GetByIdAsync(cardId, cancellationToken);
        if (card is null || card.FamilyId != familyId || card.EnvelopeId != envelopeId)
        {
            return null;
        }

        var shipment = await envelopePaymentCardShipmentRepository.GetByCardIdAsync(cardId, cancellationToken);
        if (shipment is null)
        {
            return null;
        }

        return new EnvelopePhysicalCardIssuanceDetails(
            Map(card),
            Map(shipment));
    }

    public async Task<EnvelopePhysicalCardIssuanceDetails> RefreshPhysicalCardIssuanceStatusAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var card = await envelopePaymentCardRepository.GetByIdForUpdateAsync(cardId, cancellationToken)
            ?? throw new DomainValidationException("Card was not found.");
        if (card.FamilyId != familyId || card.EnvelopeId != envelopeId)
        {
            throw new DomainValidationException("Card does not belong to the requested family/envelope.");
        }

        var shipment = await envelopePaymentCardShipmentRepository.GetByCardIdForUpdateAsync(cardId, cancellationToken)
            ?? throw new DomainValidationException("Physical card shipment was not found.");

        var providerStatus = await stripeGateway.GetCardStatusAsync(card.ProviderCardId, cancellationToken);
        var now = clock.UtcNow;

        card.ChangeStatus(providerStatus.Status, now);
        shipment.UpdateStatus(
            providerStatus.ShipmentStatus,
            providerStatus.ShipmentCarrier,
            providerStatus.ShipmentTrackingNumber,
            now);

        await envelopePaymentCardRepository.SaveChangesAsync(cancellationToken);

        return new EnvelopePhysicalCardIssuanceDetails(
            Map(card),
            Map(shipment));
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

    private static EnvelopePaymentCardShipmentDetails Map(EnvelopePaymentCardShipment shipment)
    {
        return new EnvelopePaymentCardShipmentDetails(
            shipment.Id,
            shipment.FamilyId,
            shipment.EnvelopeId,
            shipment.CardId,
            shipment.RecipientName,
            shipment.AddressLine1,
            shipment.AddressLine2,
            shipment.City,
            shipment.StateOrProvince,
            shipment.PostalCode,
            shipment.CountryCode,
            shipment.Status,
            shipment.Carrier,
            shipment.TrackingNumber,
            shipment.RequestedAtUtc,
            shipment.UpdatedAtUtc);
    }
}
