using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IParentSpendNotificationService
{
    Task<NotificationPreferenceDetails> GetPreferenceAsync(
        Guid familyId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<NotificationPreferenceDetails> UpsertPreferenceAsync(
        Guid familyId,
        string userId,
        bool emailEnabled,
        bool inAppEnabled,
        bool smsEnabled,
        CancellationToken cancellationToken = default);

    Task<SpendNotificationQueueResult> QueueSpendNotificationsAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        string webhookEventId,
        decimal amount,
        string merchant,
        decimal remainingBalance,
        CancellationToken cancellationToken = default);
}
