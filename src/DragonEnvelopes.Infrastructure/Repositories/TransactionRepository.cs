using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class TransactionRepository(DragonEnvelopesDbContext dbContext) : ITransactionRepository
{
    public async Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> AccountExistsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return dbContext.Accounts
            .AsNoTracking()
            .AnyAsync(x => x.Id == accountId, cancellationToken);
    }

    public Task<bool> EnvelopeExistsAsync(Guid envelopeId, CancellationToken cancellationToken = default)
    {
        return dbContext.Envelopes
            .AsNoTracking()
            .AnyAsync(x => x.Id == envelopeId, cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> ListTransactionsAsync(
        Guid? accountId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Transactions.AsNoTracking();
        if (accountId.HasValue)
        {
            query = query.Where(x => x.AccountId == accountId.Value);
        }

        return await query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .ToArrayAsync(cancellationToken);
    }
}
