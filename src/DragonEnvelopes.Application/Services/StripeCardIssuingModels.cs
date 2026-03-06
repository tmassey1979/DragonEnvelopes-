namespace DragonEnvelopes.Application.Services;

public sealed record StripeCardShippingAddress(
    string RecipientName,
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string CountryCode);

public sealed record StripeCardIssuanceResult(
    string ProviderCardId,
    string Status,
    string? Brand,
    string? Last4,
    string ShipmentStatus,
    string? ShipmentCarrier,
    string? ShipmentTrackingNumber);

public sealed record StripeCardStatusResult(
    string Status,
    string ShipmentStatus,
    string? ShipmentCarrier,
    string? ShipmentTrackingNumber);
