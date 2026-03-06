namespace DragonEnvelopes.Contracts.Families;

public sealed record FamilyProfileResponse(
    Guid Id,
    string Name,
    string CurrencyCode,
    string TimeZoneId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
