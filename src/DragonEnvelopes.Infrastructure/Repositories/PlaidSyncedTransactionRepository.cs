using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class PlaidSyncedTransactionRepository(DragonEnvelopesDbContext dbContext) : IPlaidSyncedTransactionRepository
{
    public Task<bool> ExistsAsync(
        Guid familyId,
        string plaidTransactionId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PlaidSyncedTransactions
            .AsNoTracking()
            .AnyAsync(
                x => x.FamilyId == familyId && x.PlaidTransactionId == plaidTransactionId,
                cancellationToken);
    }

    public async Task AddRangeAsync(
        IReadOnlyCollection<PlaidSyncedTransaction> links,
        CancellationToken cancellationToken = default)
    {
        if (links.Count == 0)
        {
            return;
        }

        await dbContext.PlaidSyncedTransactions.AddRangeAsync(links, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
