namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record FailedNotificationDispatchEventItemViewModel(
    Guid Id,
    string Channel,
    string Merchant,
    string Amount,
    int AttemptCount,
    string LastAttemptAt,
    string ErrorMessage);
