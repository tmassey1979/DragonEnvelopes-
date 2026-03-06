namespace DragonEnvelopes.Desktop.Services;

public sealed record FamilyProfileData(
    Guid Id,
    string Name,
    string CurrencyCode,
    string TimeZoneId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
