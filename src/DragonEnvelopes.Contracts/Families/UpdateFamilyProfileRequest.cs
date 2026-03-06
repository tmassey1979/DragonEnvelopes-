namespace DragonEnvelopes.Contracts.Families;

public sealed record UpdateFamilyProfileRequest(
    string Name,
    string CurrencyCode,
    string TimeZoneId);
