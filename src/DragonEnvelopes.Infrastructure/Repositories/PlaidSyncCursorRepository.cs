using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class PlaidSyncCursorRepository(DragonEnvelopesDbContext dbContext) : IPlaidSyncCursorRepository
{
    public Task<PlaidSyncCursor?> GetByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.PlaidSyncCursors
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FamilyId == familyId, cancellationToken);
    }

    public Task<PlaidSyncCursor?> GetByFamilyIdForUpdateAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.PlaidSyncCursors
            .FirstOrDefaultAsync(x => x.FamilyId == familyId, cancellationToken);
    }

    public async Task AddAsync(PlaidSyncCursor cursor, CancellationToken cancellationToken = default)
    {
        await dbContext.PlaidSyncCursors.AddAsync(cursor, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
