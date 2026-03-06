namespace DragonEnvelopes.Domain.Entities;

public sealed class EnvelopeFinancialAccount
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public Guid EnvelopeId { get; }
    public string Provider { get; private set; }
    public string ProviderFinancialAccountId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public EnvelopeFinancialAccount(
        Guid id,
        Guid familyId,
        Guid envelopeId,
        string provider,
        string providerFinancialAccountId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Envelope financial account id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (envelopeId == Guid.Empty)
        {
            throw new DomainValidationException("Envelope id is required.");
        }

        if (updatedAtUtc < createdAtUtc)
        {
            throw new DomainValidationException("Updated time cannot be before created time.");
        }

        Id = id;
        FamilyId = familyId;
        EnvelopeId = envelopeId;
        Provider = NormalizeRequired(provider, "Provider");
        ProviderFinancialAccountId = NormalizeRequired(providerFinancialAccountId, "Provider financial account id");
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Rebind(
        string provider,
        string providerFinancialAccountId,
        DateTimeOffset updatedAtUtc)
    {
        Provider = NormalizeRequired(provider, "Provider");
        ProviderFinancialAccountId = NormalizeRequired(providerFinancialAccountId, "Provider financial account id");

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
}
