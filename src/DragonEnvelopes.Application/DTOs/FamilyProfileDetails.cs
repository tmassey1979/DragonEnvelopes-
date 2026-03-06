namespace DragonEnvelopes.Application.DTOs;

public sealed record FamilyProfileDetails(
    Guid Id,
    string Name,
    string CurrencyCode,
    string TimeZoneId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
