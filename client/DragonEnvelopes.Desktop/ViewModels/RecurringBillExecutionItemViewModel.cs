namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record RecurringBillExecutionItemViewModel(
    Guid Id,
    string DueDate,
    string ExecutedAt,
    string Result,
    string TransactionId,
    string IdempotencyKey,
    string Notes);
