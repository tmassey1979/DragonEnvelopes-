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
        IReadOnlyList<TransactionSplitSnapshotViewModel> splits)
    {
        Id = id;
        AccountId = accountId;
        OccurredAt = occurredAt;
        Merchant = merchant;
        Description = description;
        Amount = amount;
        Category = category;
        EnvelopeId = envelopeId;
        Envelope = envelope;
        Splits = splits;
    }

    public Guid Id { get; }
    public Guid AccountId { get; }
    public DateTimeOffset OccurredAt { get; }
    public string Merchant { get; }
    public string Description { get; }
    public decimal Amount { get; }
    public string? Category { get; }
    public Guid? EnvelopeId { get; }
    public string Envelope { get; }
    public IReadOnlyList<TransactionSplitSnapshotViewModel> Splits { get; }
    public bool HasSplits => Splits.Count > 0;

    public string OccurredDateDisplay => OccurredAt.ToString("yyyy-MM-dd");
    public string AmountDisplay => Amount.ToString("$#,##0.00");
    public string CategoryDisplay => string.IsNullOrWhiteSpace(Category) ? "Uncategorized" : Category!;
    public string AllocationDisplay => HasSplits
        ? $"Split ({Splits.Count})"
        : string.IsNullOrWhiteSpace(Envelope) || string.Equals(Envelope, "-", StringComparison.Ordinal)
            ? "Unassigned"
            : Envelope;
}

public sealed record TransactionSplitSnapshotViewModel(
    Guid EnvelopeId,
    decimal Amount,
    string? Category,
    string? Notes);
