namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record RecurringAutoPostExecutionItemViewModel(
    Guid RecurringBillId,
    string RecurringBillName,
    string Result,
    string TransactionId,
    string Notes);
