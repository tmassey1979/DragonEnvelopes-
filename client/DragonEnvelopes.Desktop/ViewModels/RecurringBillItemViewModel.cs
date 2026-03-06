namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record RecurringBillItemViewModel(
    Guid Id,
    string Name,
    string Merchant,
    decimal AmountValue,
    string AmountDisplay,
    string Frequency,
    int DayOfMonth,
    string StartDate,
    string EndDate,
    bool IsActive);
