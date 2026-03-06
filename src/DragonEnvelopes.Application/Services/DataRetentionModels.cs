namespace DragonEnvelopes.Application.Services;

public sealed class DataRetentionOptions
{
    public bool Enabled { get; init; } = true;

    public int PollIntervalMinutes { get; init; } = 720;

    public int BatchSize { get; init; } = 500;

    public int StripeWebhookRetentionDays { get; init; } = 90;

    public int SpendNotificationRetentionDays { get; init; } = 90;
}

public sealed record DataRetentionCleanupResult(
    DateTimeOffset ExecutedAtUtc,
    DateTimeOffset StripeWebhookCutoffUtc,
    DateTimeOffset SpendNotificationCutoffUtc,
    int DeletedStripeWebhookEvents,
    int DeletedSpendNotificationEvents);
