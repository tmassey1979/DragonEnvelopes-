using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetByFamilyAndUserAsync(
        Guid familyId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<NotificationPreference?> GetByFamilyAndUserForUpdateAsync(
        Guid familyId,
        string userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
