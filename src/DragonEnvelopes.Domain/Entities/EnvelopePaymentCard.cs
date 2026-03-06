namespace DragonEnvelopes.Domain.Entities;

public sealed class EnvelopePaymentCard
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public Guid EnvelopeId { get; }
    public Guid EnvelopeFinancialAccountId { get; }
    public string Provider { get; private set; }
    public string ProviderCardId { get; private set; }
    public string Type { get; private set; }
    public string Status { get; private set; }
    public string? Brand { get; private set; }
    public string? Last4 { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public EnvelopePaymentCard(
        Guid id,
        Guid familyId,
        Guid envelopeId,
        Guid envelopeFinancialAccountId,
        string provider,
        string providerCardId,
        string type,
        string status,
        string? brand,
        string? last4,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Envelope payment card id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (envelopeId == Guid.Empty)
        {
            throw new DomainValidationException("Envelope id is required.");
        }

        if (envelopeFinancialAccountId == Guid.Empty)
        {
            throw new DomainValidationException("Envelope financial account id is required.");
        }

        if (updatedAtUtc < createdAtUtc)
        {
            throw new DomainValidationException("Updated time cannot be before created time.");
        }

        Id = id;
        FamilyId = familyId;
        EnvelopeId = envelopeId;
        EnvelopeFinancialAccountId = envelopeFinancialAccountId;
        Provider = NormalizeRequired(provider, "Provider");
        ProviderCardId = NormalizeRequired(providerCardId, "Provider card id");
        Type = NormalizeRequired(type, "Card type");
        Status = NormalizeRequired(status, "Card status");
        Brand = NormalizeNullable(brand);
        Last4 = NormalizeNullable(last4);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ChangeStatus(string status, DateTimeOffset updatedAtUtc)
    {
        Status = NormalizeRequired(status, "Card status");
        Touch(updatedAtUtc);
    }

    public void RefreshMetadata(string? brand, string? last4, DateTimeOffset updatedAtUtc)
    {
        Brand = NormalizeNullable(brand);
        Last4 = NormalizeNullable(last4);
        Touch(updatedAtUtc);
    }

    private void Touch(DateTimeOffset updatedAtUtc)
    {
        if (updatedAtUtc < CreatedAtUtc)
        {
            throw new DomainValidationException("Updated time cannot be before created time.");
        }

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
