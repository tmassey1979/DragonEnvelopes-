using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class SpendNotificationEventRepository(DragonEnvelopesDbContext dbContext) : ISpendNotificationEventRepository
{
    public async Task AddRangeAsync(
        IReadOnlyCollection<SpendNotificationEvent> events,
        CancellationToken cancellationToken = default)
    {
        await dbContext.SpendNotificationEvents.AddRangeAsync(events, cancellationToken);
    }

    public async Task<IReadOnlyList<SpendNotificationEvent>> ListDispatchableAsync(
        int maxAttempts,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SpendNotificationEvents
            .Where(x => x.Status == "Queued" && x.AttemptCount < maxAttempts)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SpendNotificationEvent>> ListFailedByFamilyAsync(
        Guid familyId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var normalizedTake = take <= 0 ? 25 : take;

        return await dbContext.SpendNotificationEvents
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId && x.Status == "Failed")
            .OrderByDescending(x => x.LastAttemptAtUtc ?? x.CreatedAtUtc)
            .Take(normalizedTake)
            .ToArrayAsync(cancellationToken);
    }

    public Task<SpendNotificationEvent?> GetByFamilyAndIdForUpdateAsync(
        Guid familyId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.SpendNotificationEvents
            .FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.Id == eventId,
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
