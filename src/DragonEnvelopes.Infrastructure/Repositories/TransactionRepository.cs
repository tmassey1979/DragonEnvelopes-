using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class TransactionRepository(DragonEnvelopesDbContext dbContext) : ITransactionRepository
{
    public async Task AddTransactionAsync(
        Transaction transaction,
        IReadOnlyList<TransactionSplitEntry> splitEntries,
        CancellationToken cancellationToken = default)
    {
        dbContext.Transactions.Add(transaction);
        if (splitEntries.Count > 0)
        {
            dbContext.TransactionSplits.AddRange(splitEntries);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> AccountExistsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return dbContext.Accounts
            .AsNoTracking()
            .AnyAsync(x => x.Id == accountId, cancellationToken);
    }

    public Task<Guid?> GetAccountFamilyIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.Id == accountId)
            .Select(x => (Guid?)x.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> AccountBelongsToFamilyAsync(
        Guid accountId,
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Accounts
            .AsNoTracking()
            .AnyAsync(x => x.Id == accountId && x.FamilyId == familyId, cancellationToken);
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

    public async Task<IReadOnlyList<TransactionSplitEntry>> ListTransactionSplitsAsync(
        IReadOnlyCollection<Guid> transactionIds,
        CancellationToken cancellationToken = default)
    {
        if (transactionIds.Count == 0)
        {
            return [];
        }

        return await dbContext.TransactionSplits
            .AsNoTracking()
            .Where(x => transactionIds.Contains(x.TransactionId))
            .OrderBy(x => x.TransactionId)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TransactionSplitEntry>> ListTransactionSplitsByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TransactionSplits
            .AsNoTracking()
            .Where(x => x.TransactionId == transactionId)
            .OrderBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task ReplaceTransactionSplitsAsync(
        Guid transactionId,
        IReadOnlyList<TransactionSplitEntry> splitEntries,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.TransactionSplits
            .Where(x => x.TransactionId == transactionId)
            .ToArrayAsync(cancellationToken);

        if (existing.Length > 0)
        {
            dbContext.TransactionSplits.RemoveRange(existing);
        }

        if (splitEntries.Count > 0)
        {
            dbContext.TransactionSplits.AddRange(splitEntries);
        }
    }

    public Task<Transaction?> GetTransactionByIdForUpdateAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Transactions
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken);
    }

    public Task<Guid?> GetTransactionFamilyIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.Id == transactionId)
            .Join(
                dbContext.Accounts.AsNoTracking(),
                transaction => transaction.AccountId,
                account => account.Id,
                (_, account) => (Guid?)account.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddTransactionsAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken = default)
    {
        if (transactions.Count == 0)
        {
            return;
        }

        dbContext.Transactions.AddRange(transactions);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
