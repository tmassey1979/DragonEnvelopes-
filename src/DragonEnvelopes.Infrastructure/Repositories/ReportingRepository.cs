using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class ReportingRepository(DragonEnvelopesDbContext dbContext) : IReportingRepository
{
    public async Task<IReadOnlyList<EnvelopeBalanceReportDetails>> ListEnvelopeBalancesAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Envelopes
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .OrderBy(x => x.Name)
            .Select(x => new EnvelopeBalanceReportDetails(
                x.Id,
                x.Name,
                x.MonthlyBudget.Amount,
                x.CurrentBalance.Amount,
                x.IsArchived))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TransactionReportRow>> ListTransactionsAsync(
        Guid familyId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toInclusive,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Transactions
            .AsNoTracking()
            .Join(
                dbContext.Accounts.AsNoTracking(),
                transaction => transaction.AccountId,
                account => account.Id,
                (transaction, account) => new { transaction, account })
            .Where(x => x.account.FamilyId == familyId)
            .Where(x => !x.transaction.DeletedAtUtc.HasValue)
            .Where(x => x.transaction.OccurredAt >= fromInclusive && x.transaction.OccurredAt <= toInclusive)
            .Select(x => new TransactionReportRow(
                x.transaction.Amount.Amount,
                x.transaction.Category,
                x.transaction.OccurredAt))
            .ToArrayAsync(cancellationToken);
    }
}
