using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface ISpendNotificationDispatchService
{
    Task<SpendNotificationDispatchResult> DispatchPendingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpendNotificationDispatchEventDetails>> ListFailedEventsAsync(
        Guid familyId,
        int take = 25,
        CancellationToken cancellationToken = default);

    Task<SpendNotificationDispatchEventDetails> RetryFailedEventAsync(
        Guid familyId,
        Guid eventId,
        CancellationToken cancellationToken = default);
}
