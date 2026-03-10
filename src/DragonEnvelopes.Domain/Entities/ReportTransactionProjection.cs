namespace DragonEnvelopes.Domain.Entities;

public sealed class ReportTransactionProjection
{
    public Guid TransactionId { get; private set; }
    public Guid FamilyId { get; private set; }
    public Guid AccountId { get; private set; }
    public decimal Amount { get; private set; }
    public string? Category { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? TransferId { get; private set; }
    public bool IsDeleted { get; private set; }
    public string LastEventId { get; private set; }
    public DateTimeOffset LastEventOccurredAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ReportTransactionProjection(
        Guid transactionId,
        Guid familyId,
        Guid accountId,
        decimal amount,
        string? category,
        DateTimeOffset occurredAt,
        Guid? transferId,
        bool isDeleted,
        string lastEventId,
        DateTimeOffset lastEventOccurredAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (transactionId == Guid.Empty)
        {
            throw new DomainValidationException("Transaction id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (accountId == Guid.Empty)
        {
            throw new DomainValidationException("Account id is required.");
        }

        if (transferId.HasValue && transferId.Value == Guid.Empty)
        {
            throw new DomainValidationException("Transfer id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(lastEventId))
        {
            throw new DomainValidationException("Last event id is required.");
        }

        TransactionId = transactionId;
        FamilyId = familyId;
        AccountId = accountId;
        Amount = amount;
        Category = NormalizeCategory(category);
        OccurredAt = occurredAt;
        TransferId = transferId;
        IsDeleted = isDeleted;
        LastEventId = lastEventId.Trim();
        LastEventOccurredAtUtc = lastEventOccurredAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Apply(
        Guid accountId,
        decimal amount,
        string? category,
        DateTimeOffset occurredAt,
        Guid? transferId,
        bool isDeleted,
        string lastEventId,
        DateTimeOffset lastEventOccurredAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (accountId == Guid.Empty)
        {
            throw new DomainValidationException("Account id is required.");
        }

        if (transferId.HasValue && transferId.Value == Guid.Empty)
        {
            throw new DomainValidationException("Transfer id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(lastEventId))
        {
            throw new DomainValidationException("Last event id is required.");
        }

        AccountId = accountId;
        Amount = amount;
        Category = NormalizeCategory(category);
        OccurredAt = occurredAt;
        TransferId = transferId;
        IsDeleted = isDeleted;
        LastEventId = lastEventId.Trim();
        LastEventOccurredAtUtc = lastEventOccurredAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string? NormalizeCategory(string? category)
    {
        return string.IsNullOrWhiteSpace(category)
            ? null
            : category.Trim();
    }
}
