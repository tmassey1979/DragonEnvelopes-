namespace DragonEnvelopes.Domain.Entities;

public sealed class PlaidAccountLink
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public Guid AccountId { get; private set; }
    public string PlaidAccountId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public PlaidAccountLink(
        Guid id,
        Guid familyId,
        Guid accountId,
        string plaidAccountId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Plaid account link id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (accountId == Guid.Empty)
        {
            throw new DomainValidationException("Account id is required.");
        }

        if (updatedAtUtc < createdAtUtc)
        {
            throw new DomainValidationException("Updated time cannot be before created time.");
        }

        Id = id;
        FamilyId = familyId;
        AccountId = accountId;
        PlaidAccountId = NormalizeRequired(plaidAccountId, "Plaid account id");
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Rebind(Guid accountId, string plaidAccountId, DateTimeOffset updatedAtUtc)
    {
        if (accountId == Guid.Empty)
        {
            throw new DomainValidationException("Account id is required.");
        }

        if (updatedAtUtc < CreatedAtUtc)
        {
            throw new DomainValidationException("Updated time cannot be before created time.");
        }

        AccountId = accountId;
        PlaidAccountId = NormalizeRequired(plaidAccountId, "Plaid account id");
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
