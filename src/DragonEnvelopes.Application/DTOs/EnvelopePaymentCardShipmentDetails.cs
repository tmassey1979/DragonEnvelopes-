namespace DragonEnvelopes.Application.DTOs;

public sealed record EnvelopePaymentCardShipmentDetails(
    Guid Id,
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId,
    string RecipientName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string StateOrProvince,
    string PostalCode,
    string CountryCode,
    string Status,
    string? Carrier,
    string? TrackingNumber,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record EnvelopePhysicalCardIssuanceDetails(
    EnvelopePaymentCardDetails Card,
    EnvelopePaymentCardShipmentDetails Shipment);
