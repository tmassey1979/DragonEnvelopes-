using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Ledger.Api.Services;

public sealed class RecurringAutoPostService(
    DragonEnvelopesDbContext dbContext,
    ITransactionService transactionService,
    IRecurringBillExecutionRepository executionRepository,
    ILogger<RecurringAutoPostService> logger) : IRecurringAutoPostService
{
    public async Task<RecurringAutoPostRunSummary> RunAsync(
        Guid? familyId = null,
        DateOnly? dueDate = null,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var effectiveDueDate = dueDate ?? DateOnly.FromDateTime(nowUtc.UtcDateTime);

        var recurringBillsQuery = dbContext.RecurringBills
            .AsNoTracking()
            .Where(static recurringBill => recurringBill.IsActive);

        if (familyId.HasValue)
        {
            recurringBillsQuery = recurringBillsQuery.Where(recurringBill => recurringBill.FamilyId == familyId.Value);
        }

        var recurringBills = await recurringBillsQuery.ToArrayAsync(cancellationToken);
        var executionItems = new List<RecurringAutoPostExecutionItem>();

        var dueBillCount = 0;
        var postedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;
        var alreadyProcessedCount = 0;

        foreach (var recurringBill in recurringBills)
        {
            if (!IsDueOnDate(recurringBill, effectiveDueDate))
            {
                continue;
            }

            dueBillCount += 1;

            if (await executionRepository.HasExecutionAsync(recurringBill.Id, effectiveDueDate, cancellationToken))
            {
                alreadyProcessedCount += 1;
                executionItems.Add(new RecurringAutoPostExecutionItem(
                    recurringBill.Id,
                    recurringBill.Name,
                    "AlreadyProcessed",
                    TransactionId: null,
                    "Execution already exists for due date."));
                continue;
            }

            var accountId = await dbContext.Accounts
                .AsNoTracking()
                .Where(account => account.FamilyId == recurringBill.FamilyId)
                .OrderBy(account => account.Id)
                .Select(account => (Guid?)account.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!accountId.HasValue)
            {
                const string notes = "No account found for family.";
                await executionRepository.AddAsync(
                    new RecurringBillExecution(
                        Guid.NewGuid(),
                        recurringBill.Id,
                        recurringBill.FamilyId,
                        effectiveDueDate,
                        nowUtc,
                        transactionId: null,
                        result: "SkippedNoAccount",
                        notes: notes),
                    cancellationToken);

                skippedCount += 1;
                executionItems.Add(new RecurringAutoPostExecutionItem(
                    recurringBill.Id,
                    recurringBill.Name,
                    "SkippedNoAccount",
                    TransactionId: null,
                    notes));
                continue;
            }

            try
            {
                var transaction = await transactionService.CreateAsync(
                    accountId.Value,
                    -recurringBill.Amount.Amount,
                    $"Recurring bill auto-post: {recurringBill.Name}",
                    recurringBill.Merchant,
                    new DateTimeOffset(effectiveDueDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                    "Recurring Bill",
                    envelopeId: null,
                    hasSplits: false,
                    splits: null,
                    cancellationToken);

                await executionRepository.AddAsync(
                    new RecurringBillExecution(
                        Guid.NewGuid(),
                        recurringBill.Id,
                        recurringBill.FamilyId,
                        effectiveDueDate,
                        nowUtc,
                        transaction.Id,
                        result: "Posted",
                        notes: null),
                    cancellationToken);

                postedCount += 1;
                executionItems.Add(new RecurringAutoPostExecutionItem(
                    recurringBill.Id,
                    recurringBill.Name,
                    "Posted",
                    transaction.Id,
                    Notes: null));
            }
            catch (Exception ex)
            {
                var notes = Truncate(ex.Message, 500);
                await executionRepository.AddAsync(
                    new RecurringBillExecution(
                        Guid.NewGuid(),
                        recurringBill.Id,
                        recurringBill.FamilyId,
                        effectiveDueDate,
                        nowUtc,
                        transactionId: null,
                        result: "Failed",
                        notes: notes),
                    cancellationToken);

                failedCount += 1;
                executionItems.Add(new RecurringAutoPostExecutionItem(
                    recurringBill.Id,
                    recurringBill.Name,
                    "Failed",
                    TransactionId: null,
                    notes));

                logger.LogWarning(
                    ex,
                    "Recurring auto-post failed for bill {RecurringBillId} on {DueDate}.",
                    recurringBill.Id,
                    effectiveDueDate);
            }
        }

        return new RecurringAutoPostRunSummary(
            familyId,
            effectiveDueDate,
            dueBillCount,
            postedCount,
            skippedCount,
            failedCount,
            alreadyProcessedCount,
            executionItems
                .OrderBy(static item => item.RecurringBillName, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static bool IsDueOnDate(RecurringBill bill, DateOnly date)
    {
        if (!bill.IsActive || date < bill.StartDate)
        {
            return false;
        }

        if (bill.EndDate.HasValue && date > bill.EndDate.Value)
        {
            return false;
        }

        return bill.Frequency switch
        {
            RecurringBillFrequency.Monthly => date.Day == Math.Min(bill.DayOfMonth, DateTime.DaysInMonth(date.Year, date.Month)),
            RecurringBillFrequency.Weekly => (date.DayNumber - bill.StartDate.DayNumber) % 7 == 0,
            RecurringBillFrequency.BiWeekly => (date.DayNumber - bill.StartDate.DayNumber) % 14 == 0,
            _ => false
        };
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}
