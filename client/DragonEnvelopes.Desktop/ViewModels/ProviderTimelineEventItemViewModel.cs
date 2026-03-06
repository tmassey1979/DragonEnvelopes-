namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record ProviderTimelineEventItemViewModel(
    string Source,
    string EventType,
    string Status,
    string OccurredAt,
    string Summary,
    string Detail);
