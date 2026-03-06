using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class FamilyFinancialProfileRepository(DragonEnvelopesDbContext dbContext) : IFamilyFinancialProfileRepository
{
    public Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Families
            .AsNoTracking()
            .AnyAsync(x => x.Id == familyId, cancellationToken);
    }

    public Task<FamilyFinancialProfile?> GetByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.FamilyFinancialProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FamilyId == familyId, cancellationToken);
    }

    public Task<FamilyFinancialProfile?> GetByFamilyIdForUpdateAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.FamilyFinancialProfiles
            .FirstOrDefaultAsync(x => x.FamilyId == familyId, cancellationToken);
    }

    public async Task<IReadOnlyList<FamilyFinancialProfile>> ListPlaidConnectedAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.FamilyFinancialProfiles
            .AsNoTracking()
            .Where(x => x.PlaidConnected && x.PlaidAccessToken != null)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(FamilyFinancialProfile profile, CancellationToken cancellationToken = default)
    {
        dbContext.FamilyFinancialProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
