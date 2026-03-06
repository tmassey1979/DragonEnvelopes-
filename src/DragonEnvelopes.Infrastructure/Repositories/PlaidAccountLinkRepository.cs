using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class PlaidAccountLinkRepository(DragonEnvelopesDbContext dbContext) : IPlaidAccountLinkRepository
{
    public Task<PlaidAccountLink?> GetByFamilyAndPlaidAccountIdAsync(
        Guid familyId,
        string plaidAccountId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PlaidAccountLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.PlaidAccountId == plaidAccountId,
                cancellationToken);
    }

    public Task<PlaidAccountLink?> GetByFamilyAndPlaidAccountIdForUpdateAsync(
        Guid familyId,
        string plaidAccountId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PlaidAccountLinks
            .FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.PlaidAccountId == plaidAccountId,
                cancellationToken);
    }

    public Task<PlaidAccountLink?> GetByFamilyAndIdForUpdateAsync(
        Guid familyId,
        Guid linkId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PlaidAccountLinks
            .FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.Id == linkId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<PlaidAccountLink>> ListByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PlaidAccountLinks
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(PlaidAccountLink link, CancellationToken cancellationToken = default)
    {
        await dbContext.PlaidAccountLinks.AddAsync(link, cancellationToken);
    }

    public Task DeleteAsync(PlaidAccountLink link, CancellationToken cancellationToken = default)
    {
        dbContext.PlaidAccountLinks.Remove(link);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
