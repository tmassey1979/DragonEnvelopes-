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
        string envelope)
    {
        Id = id;
        AccountId = accountId;
        OccurredAt = occurredAt;
        Merchant = merchant;
        Description = description;
        Amount = amount;
        Category = category;
        Envelope = envelope;
    }

    public Guid Id { get; }
    public Guid AccountId { get; }
    public DateTimeOffset OccurredAt { get; }
    public string Merchant { get; }
    public string Description { get; }
    public decimal Amount { get; }
    public string? Category { get; }
    public string Envelope { get; }

    public string OccurredDateDisplay => OccurredAt.ToString("yyyy-MM-dd");
    public string AmountDisplay => Amount.ToString("$#,##0.00");
    public string CategoryDisplay => string.IsNullOrWhiteSpace(Category) ? "Uncategorized" : Category!;
}
