namespace DragonEnvelopes.Domain.Entities;

public sealed class EnvelopePaymentCardControl
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public Guid EnvelopeId { get; }
    public Guid CardId { get; }
    public decimal? DailyLimitAmount { get; private set; }
    public string? AllowedMerchantCategoriesJson { get; private set; }
    public string? AllowedMerchantNamesJson { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public EnvelopePaymentCardControl(
        Guid id,
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        decimal? dailyLimitAmount,
        string? allowedMerchantCategoriesJson,
        string? allowedMerchantNamesJson,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Card control id is required.");
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

        if (dailyLimitAmount.HasValue && dailyLimitAmount.Value < 0m)
        {
            throw new DomainValidationException("Daily limit amount cannot be negative.");
        }

        if (updatedAtUtc < createdAtUtc)
        {
            throw new DomainValidationException("Updated time cannot be before created time.");
        }

        Id = id;
        FamilyId = familyId;
        EnvelopeId = envelopeId;
        CardId = cardId;
        DailyLimitAmount = dailyLimitAmount;
        AllowedMerchantCategoriesJson = NormalizeNullable(allowedMerchantCategoriesJson);
        AllowedMerchantNamesJson = NormalizeNullable(allowedMerchantNamesJson);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Update(
        decimal? dailyLimitAmount,
        string? allowedMerchantCategoriesJson,
        string? allowedMerchantNamesJson,
        DateTimeOffset updatedAtUtc)
    {
        if (dailyLimitAmount.HasValue && dailyLimitAmount.Value < 0m)
        {
            throw new DomainValidationException("Daily limit amount cannot be negative.");
        }

        if (updatedAtUtc < CreatedAtUtc)
        {
            throw new DomainValidationException("Updated time cannot be before created time.");
        }

        DailyLimitAmount = dailyLimitAmount;
        AllowedMerchantCategoriesJson = NormalizeNullable(allowedMerchantCategoriesJson);
        AllowedMerchantNamesJson = NormalizeNullable(allowedMerchantNamesJson);
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
