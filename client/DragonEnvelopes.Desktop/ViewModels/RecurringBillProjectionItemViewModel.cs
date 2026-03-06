namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record RecurringBillProjectionItemViewModel(
    Guid RecurringBillId,
    string Name,
    string Merchant,
    string AmountDisplay,
    string DueDate);
