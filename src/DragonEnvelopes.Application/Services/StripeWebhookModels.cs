namespace DragonEnvelopes.Application.Services;

public sealed class StripeWebhookOptions
{
    public bool Enabled { get; set; }
    public string SigningSecret { get; set; } = string.Empty;
    public int SignatureToleranceSeconds { get; set; } = 300;
}

public sealed record StripeWebhookProcessResult(
    string Outcome,
    string? EventId,
    string? EventType,
    string? Message)
{
    public static StripeWebhookProcessResult InvalidSignature() =>
        new("InvalidSignature", null, null, "Stripe signature verification failed.");

    public static StripeWebhookProcessResult Disabled() =>
        new("Disabled", null, null, "Stripe webhook processing is disabled.");

    public static StripeWebhookProcessResult Duplicate(string eventId, string eventType) =>
        new("Duplicate", eventId, eventType, null);

    public static StripeWebhookProcessResult Processed(string eventId, string eventType) =>
        new("Processed", eventId, eventType, null);

    public static StripeWebhookProcessResult Ignored(string eventId, string eventType, string reason) =>
        new("Ignored", eventId, eventType, reason);

    public static StripeWebhookProcessResult Failed(string eventId, string eventType, string message) =>
        new("Failed", eventId, eventType, message);
}

public sealed record StripeWebhookReplayResult(
    Guid Id,
    Guid? FamilyId,
    string Status,
    string Outcome,
    DateTimeOffset ProcessedAtUtc,
    string? ErrorMessage);
