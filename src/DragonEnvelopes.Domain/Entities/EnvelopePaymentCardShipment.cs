namespace DragonEnvelopes.Domain.Entities;

public sealed class EnvelopePaymentCardShipment
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public Guid EnvelopeId { get; }
    public Guid CardId { get; }
    public string RecipientName { get; private set; }
    public string AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; }
    public string StateOrProvince { get; private set; }
    public string PostalCode { get; private set; }
    public string CountryCode { get; private set; }
    public string Status { get; private set; }
    public string? Carrier { get; private set; }
    public string? TrackingNumber { get; private set; }
    public DateTimeOffset RequestedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public EnvelopePaymentCardShipment(
        Guid id,
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        string recipientName,
        string addressLine1,
        string? addressLine2,
        string city,
        string stateOrProvince,
        string postalCode,
        string countryCode,
        string status,
        string? carrier,
        string? trackingNumber,
        DateTimeOffset requestedAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Card shipment id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (envelopeId == Guid.Empty)
        {
            throw new DomainValidationException("Envelope id is required.");
        }

        if (cardId == Guid.Empty)
        {
            throw new DomainValidationException("Card id is required.");
        }

        if (updatedAtUtc < requestedAtUtc)
        {
            throw new DomainValidationException("Updated time cannot be before requested time.");
        }

        Id = id;
        FamilyId = familyId;
        EnvelopeId = envelopeId;
        CardId = cardId;
        RecipientName = NormalizeRequired(recipientName, "Recipient name");
        AddressLine1 = NormalizeRequired(addressLine1, "Address line 1");
        AddressLine2 = NormalizeNullable(addressLine2);
        City = NormalizeRequired(city, "City");
        StateOrProvince = NormalizeRequired(stateOrProvince, "State or province");
        PostalCode = NormalizeRequired(postalCode, "Postal code");
        CountryCode = NormalizeRequired(countryCode, "Country code").ToUpperInvariant();
        Status = NormalizeRequired(status, "Shipment status");
        Carrier = NormalizeNullable(carrier);
        TrackingNumber = NormalizeNullable(trackingNumber);
        RequestedAtUtc = requestedAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void UpdateStatus(
        string status,
        string? carrier,
        string? trackingNumber,
        DateTimeOffset updatedAtUtc)
    {
        if (updatedAtUtc < RequestedAtUtc)
        {
            throw new DomainValidationException("Updated time cannot be before requested time.");
        }

        Status = NormalizeRequired(status, "Shipment status");
        Carrier = NormalizeNullable(carrier);
        TrackingNumber = NormalizeNullable(trackingNumber);
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string NormalizeRequired(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{field} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
