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
    public Guid? TransferId { get; }
    public Guid? TransferCounterpartyEnvelopeId { get; }
    public string? TransferDirection { get; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public string? DeletedByUserId { get; private set; }
    public IReadOnlyCollection<TransactionSplit> Splits => _splits.AsReadOnly();
    public bool IsTransfer => TransferId.HasValue;

    public Transaction(
        Guid id,
        Guid accountId,
        Money amount,
        string description,
        string merchant,
        DateTimeOffset occurredAt,
        string? category = null,
        Guid? envelopeId = null,
        Guid? transferId = null,
        Guid? transferCounterpartyEnvelopeId = null,
        string? transferDirection = null)
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
        TransferId = transferId;
        TransferCounterpartyEnvelopeId = transferCounterpartyEnvelopeId;
        TransferDirection = NormalizeOptional(transferDirection);
        ValidateTransferMetadata(EnvelopeId, TransferId, TransferCounterpartyEnvelopeId, TransferDirection);
        DeletedAtUtc = null;
        DeletedByUserId = null;

    }

    public static Transaction CreateWithSplits(
        Guid id,
        Guid accountId,
        Money amount,
        string description,
        string merchant,
        DateTimeOffset occurredAt,
        IEnumerable<TransactionSplit> splits,
        string? category = null)
    {
        var transaction = new Transaction(
            id,
            accountId,
            amount,
            description,
            merchant,
            occurredAt,
            category,
            envelopeId: null);

        transaction.SetSplits(splits);
        return transaction;
    }

    public void SetCategory(string? category)
    {
        Category = NormalizeOptional(category);
    }

    public void UpdateMetadata(string description, string merchant, string? category)
    {
        Description = ValidateText(description, "Transaction description");
        Merchant = ValidateText(merchant, "Transaction merchant");
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

    public void SoftDelete(DateTimeOffset deletedAtUtc, string? deletedByUserId)
    {
        if (DeletedAtUtc.HasValue)
        {
            throw new DomainValidationException("Transaction is already deleted.");
        }

        DeletedAtUtc = deletedAtUtc;
        DeletedByUserId = NormalizeOptional(deletedByUserId);
    }

    public void Restore()
    {
        if (!DeletedAtUtc.HasValue)
        {
            throw new DomainValidationException("Transaction is not deleted.");
        }

        DeletedAtUtc = null;
        DeletedByUserId = null;
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

    private static void ValidateTransferMetadata(
        Guid? envelopeId,
        Guid? transferId,
        Guid? transferCounterpartyEnvelopeId,
        string? transferDirection)
    {
        if (!transferId.HasValue)
        {
            if (transferCounterpartyEnvelopeId.HasValue || !string.IsNullOrWhiteSpace(transferDirection))
            {
                throw new DomainValidationException("Transfer metadata is invalid.");
            }

            return;
        }

        if (!envelopeId.HasValue)
        {
            throw new DomainValidationException("Transfer transactions must reference an envelope.");
        }

        if (!transferCounterpartyEnvelopeId.HasValue)
        {
            throw new DomainValidationException("Transfer counterparty envelope id is required.");
        }

        if (transferCounterpartyEnvelopeId.Value == envelopeId.Value)
        {
            throw new DomainValidationException("Transfer counterparty envelope id must differ from envelope id.");
        }

        if (string.IsNullOrWhiteSpace(transferDirection))
        {
            throw new DomainValidationException("Transfer direction is required.");
        }

        var normalizedDirection = transferDirection.Trim();
        if (!string.Equals(normalizedDirection, "Debit", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedDirection, "Credit", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("Transfer direction is invalid.");
        }
    }
}
