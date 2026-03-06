namespace DragonEnvelopes.Application.DTOs;

public sealed record NotificationPreferenceDetails(
    Guid FamilyId,
    string UserId,
    bool EmailEnabled,
    bool InAppEnabled,
    bool SmsEnabled,
    DateTimeOffset UpdatedAtUtc);

public sealed record SpendNotificationQueueResult(
    int GeneratedCount);

public sealed record SpendNotificationDispatchResult(
    int SentCount,
    int FailedCount);

public sealed record SpendNotificationDispatchEventDetails(
    Guid Id,
    Guid FamilyId,
    string UserId,
    Guid EnvelopeId,
    Guid CardId,
    string Channel,
    decimal Amount,
    string Merchant,
    string Status,
    int AttemptCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastAttemptAtUtc,
    DateTimeOffset? SentAtUtc,
    string? ErrorMessage);
