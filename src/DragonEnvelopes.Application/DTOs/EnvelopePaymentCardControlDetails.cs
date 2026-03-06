namespace DragonEnvelopes.Application.DTOs;

public sealed record EnvelopePaymentCardControlDetails(
    Guid Id,
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId,
    decimal? DailyLimitAmount,
    IReadOnlyList<string> AllowedMerchantCategories,
    IReadOnlyList<string> AllowedMerchantNames,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
