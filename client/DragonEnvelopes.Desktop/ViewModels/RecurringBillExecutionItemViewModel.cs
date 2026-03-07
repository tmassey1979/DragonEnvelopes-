namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record RecurringBillExecutionItemViewModel(
    Guid Id,
    DateOnly DueDate,
    DateTimeOffset ExecutedAtUtc,
    string Result,
    string TransactionId,
    string IdempotencyKey,
    string Notes)
{
    public string DueDateDisplay => DueDate.ToString("yyyy-MM-dd");

    public string ExecutedAtDisplay => ExecutedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public string ExecutedAtIso => ExecutedAtUtc.ToString("O");
}
