namespace DragonEnvelopes.Domain.Entities;

public sealed class PlaidBalanceSnapshot
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public Guid AccountId { get; }
    public string PlaidAccountId { get; }
    public decimal InternalBalanceBefore { get; }
    public decimal ProviderBalance { get; }
    public decimal InternalBalanceAfter { get; }
    public decimal DriftAmount { get; }
    public DateTimeOffset RefreshedAtUtc { get; }

    public PlaidBalanceSnapshot(
        Guid id,
        Guid familyId,
        Guid accountId,
        string plaidAccountId,
        decimal internalBalanceBefore,
        decimal providerBalance,
        decimal internalBalanceAfter,
        decimal driftAmount,
        DateTimeOffset refreshedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Plaid balance snapshot id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (accountId == Guid.Empty)
        {
            throw new DomainValidationException("Account id is required.");
        }

        Id = id;
        FamilyId = familyId;
        AccountId = accountId;
        PlaidAccountId = NormalizeRequired(plaidAccountId, "Plaid account id");
        InternalBalanceBefore = internalBalanceBefore;
        ProviderBalance = providerBalance;
        InternalBalanceAfter = internalBalanceAfter;
        DriftAmount = driftAmount;
        RefreshedAtUtc = refreshedAtUtc;
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
