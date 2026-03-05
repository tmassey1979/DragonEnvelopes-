using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Domain.Entities;

public sealed class Transaction
{
    private readonly List<TransactionSplit> _splits = [];

    public Guid Id { get; }
    public Guid AccountId { get; }
    public Money Amount { get; }
    public string Description { get; private set; }
    public string Merchant { get; private set; }
    public DateTimeOffset OccurredAt { get; }
    public string? Category { get; private set; }
    public Guid? EnvelopeId { get; private set; }
    public IReadOnlyCollection<TransactionSplit> Splits => _splits.AsReadOnly();

    public Transaction(
        Guid id,
        Guid accountId,
        Money amount,
        string description,
        string merchant,
        DateTimeOffset occurredAt,
        string? category = null,
        Guid? envelopeId = null,
        IEnumerable<TransactionSplit>? splits = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Transaction id is required.");
        }

        if (accountId == Guid.Empty)
        {
            throw new DomainValidationException("Account id is required.");
        }

        if (amount.IsZero)
        {
            throw new DomainValidationException("Transaction amount cannot be zero.");
        }

        Id = id;
        AccountId = accountId;
        Amount = amount;
        Description = ValidateText(description, "Transaction description");
        Merchant = ValidateText(merchant, "Transaction merchant");
        OccurredAt = occurredAt;
        Category = NormalizeOptional(category);
        EnvelopeId = envelopeId;

        if (splits is not null)
        {
            SetSplits(splits);
        }
    }

    public void SetCategory(string? category)
    {
        Category = NormalizeOptional(category);
    }

    public void AssignEnvelope(Guid? envelopeId)
    {
        if (_splits.Count > 0 && envelopeId.HasValue)
        {
            throw new DomainValidationException("Cannot set single envelope when transaction has splits.");
        }

        EnvelopeId = envelopeId;
    }

    public void SetSplits(IEnumerable<TransactionSplit> splits)
    {
        var splitList = splits.ToList();
        if (splitList.Count == 0)
        {
            _splits.Clear();
            return;
        }

        var total = splitList.Aggregate(Money.Zero, static (sum, split) => sum + split.Amount);
        if (total != Amount)
        {
            throw new DomainValidationException("Split totals must equal transaction amount.");
        }

        _splits.Clear();
        _splits.AddRange(splitList);
        EnvelopeId = null;
    }

    private static string ValidateText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

