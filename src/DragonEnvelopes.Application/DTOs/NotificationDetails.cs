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
