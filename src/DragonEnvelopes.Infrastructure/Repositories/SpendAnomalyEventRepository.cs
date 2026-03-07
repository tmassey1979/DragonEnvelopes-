using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class SpendAnomalyEventRepository(DragonEnvelopesDbContext dbContext) : ISpendAnomalyEventRepository
{
    public Task<bool> ExistsForTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.SpendAnomalyEvents
            .AsNoTracking()
            .AnyAsync(x => x.TransactionId == transactionId, cancellationToken);
    }

    public async Task AddAsync(
        SpendAnomalyEvent anomalyEvent,
        CancellationToken cancellationToken = default)
    {
        await dbContext.SpendAnomalyEvents.AddAsync(anomalyEvent, cancellationToken);
    }

    public async Task<IReadOnlyList<SpendAnomalyEvent>> ListByFamilyAsync(
        Guid familyId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var normalizedTake = take <= 0 ? 50 : take;

        return await dbContext.SpendAnomalyEvents
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .OrderByDescending(x => x.DetectedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(normalizedTake)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SpendAnomalySample>> ListRecentSpendSamplesAsync(
        Guid familyId,
        DateTimeOffset occurredSinceUtc,
        Guid? excludeTransactionId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var normalizedTake = take <= 0 ? 100 : take;

        var query = dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                !transaction.DeletedAtUtc.HasValue
                && transaction.Amount.Amount < 0m
                && transaction.OccurredAt >= occurredSinceUtc)
            .Join(
                dbContext.Accounts.AsNoTracking(),
                transaction => transaction.AccountId,
                account => account.Id,
                (transaction, account) => new
                {
                    Transaction = transaction,
                    Account = account
                })
            .Where(x => x.Account.FamilyId == familyId);

        if (excludeTransactionId.HasValue)
        {
            query = query.Where(x => x.Transaction.Id != excludeTransactionId.Value);
        }

        return await query
            .OrderByDescending(x => x.Transaction.OccurredAt)
            .Take(normalizedTake)
            .Select(x => new SpendAnomalySample(
                x.Transaction.Id,
                x.Transaction.Merchant,
                x.Transaction.Amount.Amount,
                x.Transaction.OccurredAt))
            .ToArrayAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
