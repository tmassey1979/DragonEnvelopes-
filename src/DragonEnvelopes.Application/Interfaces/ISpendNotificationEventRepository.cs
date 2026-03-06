using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface ISpendNotificationEventRepository
{
    Task AddRangeAsync(
        IReadOnlyCollection<SpendNotificationEvent> events,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpendNotificationEvent>> ListDispatchableAsync(
        int maxAttempts,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpendNotificationEvent>> ListFailedByFamilyAsync(
        Guid familyId,
        int take,
        CancellationToken cancellationToken = default);

    Task<SpendNotificationEvent?> GetByFamilyAndIdForUpdateAsync(
        Guid familyId,
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
