namespace DragonEnvelopes.Domain.Entities;

public sealed class PlaidSyncedTransaction
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public string PlaidTransactionId { get; }
    public Guid AccountId { get; }
    public Guid TransactionId { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    public PlaidSyncedTransaction(
        Guid id,
        Guid familyId,
        string plaidTransactionId,
        Guid accountId,
        Guid transactionId,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Plaid synced transaction id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (accountId == Guid.Empty)
        {
            throw new DomainValidationException("Account id is required.");
        }

        if (transactionId == Guid.Empty)
        {
            throw new DomainValidationException("Transaction id is required.");
        }

        Id = id;
        FamilyId = familyId;
        PlaidTransactionId = NormalizeRequired(plaidTransactionId, "Plaid transaction id");
        AccountId = accountId;
        TransactionId = transactionId;
        OccurredAtUtc = occurredAtUtc;
        CreatedAtUtc = createdAtUtc;
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
