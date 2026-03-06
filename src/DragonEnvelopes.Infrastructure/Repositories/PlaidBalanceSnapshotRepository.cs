using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class PlaidBalanceSnapshotRepository(DragonEnvelopesDbContext dbContext) : IPlaidBalanceSnapshotRepository
{
    public async Task AddRangeAsync(
        IReadOnlyCollection<PlaidBalanceSnapshot> snapshots,
        CancellationToken cancellationToken = default)
    {
        if (snapshots.Count == 0)
        {
            return;
        }

        await dbContext.PlaidBalanceSnapshots.AddRangeAsync(snapshots, cancellationToken);
    }

    public async Task<IReadOnlyList<PlaidBalanceSnapshot>> ListRecentByFamilyAsync(
        Guid familyId,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PlaidBalanceSnapshots
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .OrderByDescending(x => x.RefreshedAtUtc)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
