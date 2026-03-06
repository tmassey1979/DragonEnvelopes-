using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class NotificationPreferenceRepository(DragonEnvelopesDbContext dbContext) : INotificationPreferenceRepository
{
    public Task<NotificationPreference?> GetByFamilyAndUserAsync(
        Guid familyId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.UserId == userId,
                cancellationToken);
    }

    public Task<NotificationPreference?> GetByFamilyAndUserForUpdateAsync(
        Guid familyId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.NotificationPreferences
            .FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.UserId == userId,
                cancellationToken);
    }

    public async Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        await dbContext.NotificationPreferences.AddAsync(preference, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
