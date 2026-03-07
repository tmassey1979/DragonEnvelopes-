namespace DragonEnvelopes.Desktop.ViewModels;

public sealed class TransactionListItemViewModel
{
    public TransactionListItemViewModel(
        Guid id,
        Guid accountId,
        DateTimeOffset occurredAt,
        string merchant,
        string description,
        decimal amount,
        string? category,
        Guid? envelopeId,
        string envelope,
        IReadOnlyList<TransactionSplitSnapshotViewModel> splits,
        DateTimeOffset? deletedAtUtc = null,
        string? deletedByUserId = null,
        Guid? transferId = null,
        Guid? transferCounterpartyEnvelopeId = null,
        string? transferDirection = null,
        string? transferCounterpartyEnvelopeName = null,
        string? approvalStatus = null)
    {
        Id = id;
        AccountId = accountId;
        OccurredAt = occurredAt;
        Merchant = merchant;
        Description = description;
        Amount = amount;
        Category = category;
        EnvelopeId = envelopeId;
        TransferId = transferId;
        TransferCounterpartyEnvelopeId = transferCounterpartyEnvelopeId;
        TransferDirection = transferDirection;
        TransferCounterpartyEnvelopeName = transferCounterpartyEnvelopeName;
        ApprovalStatus = NormalizeApprovalStatus(approvalStatus);
        Envelope = envelope;
        Splits = splits;
        DeletedAtUtc = deletedAtUtc;
        DeletedByUserId = deletedByUserId;
    }

    public Guid Id { get; }
    public Guid AccountId { get; }
    public DateTimeOffset OccurredAt { get; }
    public string Merchant { get; }
    public string Description { get; }
    public decimal Amount { get; }
    public string? Category { get; }
    public Guid? EnvelopeId { get; }
    public Guid? TransferId { get; }
    public Guid? TransferCounterpartyEnvelopeId { get; }
    public string? TransferDirection { get; }
    public string? TransferCounterpartyEnvelopeName { get; }
    public string? ApprovalStatus { get; private set; }
    public string Envelope { get; }
    public IReadOnlyList<TransactionSplitSnapshotViewModel> Splits { get; }
    public DateTimeOffset? DeletedAtUtc { get; }
    public string? DeletedByUserId { get; }
    public bool HasSplits => Splits.Count > 0;
    public bool IsTransfer => TransferId.HasValue;
    public bool IsDeleted => DeletedAtUtc.HasValue;

    public string OccurredDateDisplay => OccurredAt.ToString("yyyy-MM-dd");
    public string DeletedAtDateDisplay => DeletedAtUtc?.ToString("yyyy-MM-dd") ?? "-";
    public string AmountDisplay => Amount.ToString("$#,##0.00");
    public string CategoryDisplay => string.IsNullOrWhiteSpace(Category) ? "Uncategorized" : Category!;
    public string TransferDisplay => !IsTransfer
        ? "-"
        : string.Equals(TransferDirection, "Debit", StringComparison.OrdinalIgnoreCase)
            ? $"Out -> {TransferCounterpartyEnvelopeName ?? "Unknown"}"
            : $"In <- {TransferCounterpartyEnvelopeName ?? "Unknown"}";
    public string StatusBadgeText => IsDeleted
        ? "Deleted"
        : ApprovalStatus ?? "Posted";
    public string AllocationDisplay => HasSplits
        ? $"Split ({Splits.Count})"
        : IsTransfer
            ? TransferDisplay
        : string.IsNullOrWhiteSpace(Envelope) || string.Equals(Envelope, "-", StringComparison.Ordinal)
            ? "Unassigned"
            : Envelope;

    public void SetApprovalStatus(string? approvalStatus)
    {
        ApprovalStatus = NormalizeApprovalStatus(approvalStatus);
    }

    private static string? NormalizeApprovalStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? null
            : status.Trim();
    }
}

public sealed record TransactionSplitSnapshotViewModel(
    Guid EnvelopeId,
    decimal Amount,
    string? Category,
    string? Notes);
