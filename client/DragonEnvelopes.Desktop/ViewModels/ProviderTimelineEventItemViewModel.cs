namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record ProviderTimelineEventItemViewModel(
    string Source,
    string EventType,
    string Status,
    string OccurredAt,
    string Summary,
    string Detail,
    Guid? StripeWebhookEventId,
    Guid? PlaidWebhookEventId,
    Guid? NotificationDispatchEventId,
    Guid? ReconciliationAlertEventId,
    bool CanReplayNotification,
    bool CanReplayStripeWebhook)
{
    public string StripeWebhookEventIdDisplay => StripeWebhookEventId?.ToString("D") ?? "-";
    public string PlaidWebhookEventIdDisplay => PlaidWebhookEventId?.ToString("D") ?? "-";

    public string NotificationDispatchEventIdDisplay => NotificationDispatchEventId?.ToString("D") ?? "-";
    public string ReconciliationAlertEventIdDisplay => ReconciliationAlertEventId?.ToString("D") ?? "-";

    public bool CanReplayAny => CanReplayNotification || CanReplayStripeWebhook;

    public string ReplayEligibility => CanReplayAny ? "Replayable" : "-";
}
