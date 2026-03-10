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
        var hasProjectionData = await HasAppliedProjectionDataAsync(familyId, cancellationToken);
        if (hasProjectionData)
        {
            return await dbContext.ReportEnvelopeBalanceProjections
                .AsNoTracking()
                .Where(x => x.FamilyId == familyId)
                .OrderBy(x => x.EnvelopeName)
                .Select(x => new EnvelopeBalanceReportDetails(
                    x.EnvelopeId,
                    x.EnvelopeName,
                    x.MonthlyBudget,
                    x.CurrentBalance,
                    x.IsArchived))
                .ToArrayAsync(cancellationToken);
        }

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
        var hasProjectionData = await HasAppliedProjectionDataAsync(familyId, cancellationToken);
        if (hasProjectionData)
        {
            return await dbContext.ReportTransactionProjections
                .AsNoTracking()
                .Where(x => x.FamilyId == familyId)
                .Where(x => !x.IsDeleted)
                .Where(x => x.OccurredAt >= fromInclusive && x.OccurredAt <= toInclusive)
                .Select(x => new TransactionReportRow(
                    x.Amount,
                    x.Category,
                    x.OccurredAt,
                    x.TransferId))
                .ToArrayAsync(cancellationToken);
        }

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
                x.transaction.OccurredAt,
                x.transaction.TransferId))
            .ToArrayAsync(cancellationToken);
    }

    private Task<bool> HasAppliedProjectionDataAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return dbContext.ReportProjectionAppliedEvents
            .AsNoTracking()
            .AnyAsync(
                x => x.FamilyId == familyId
                     && x.ProcessingStatus == "Applied",
                cancellationToken);
    }
}
