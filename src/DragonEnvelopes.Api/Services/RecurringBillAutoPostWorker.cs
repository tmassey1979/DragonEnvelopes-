using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Api.Services;

public sealed class RecurringBillAutoPostWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<RecurringBillAutoPostWorker> logger) : BackgroundService
{
    private static readonly TimeSpan LoopInterval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(LoopInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessDueRecurringBillsAsync(stoppingToken);
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task ProcessDueRecurringBillsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            var transactionService = scope.ServiceProvider.GetRequiredService<ITransactionService>();
            var executionRepository = scope.ServiceProvider.GetRequiredService<IRecurringBillExecutionRepository>();

            var nowUtc = DateTimeOffset.UtcNow;
            var dueDate = DateOnly.FromDateTime(nowUtc.UtcDateTime);

            var recurringBills = await dbContext.RecurringBills
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToArrayAsync(cancellationToken);

            foreach (var recurringBill in recurringBills)
            {
                if (!IsDueOnDate(recurringBill, dueDate))
                {
                    continue;
                }

                if (await executionRepository.HasExecutionAsync(recurringBill.Id, dueDate, cancellationToken))
                {
                    continue;
                }

                var accountId = await dbContext.Accounts
                    .AsNoTracking()
                    .Where(x => x.FamilyId == recurringBill.FamilyId)
                    .OrderBy(x => x.Id)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!accountId.HasValue)
                {
                    await executionRepository.AddAsync(
                        new RecurringBillExecution(
                            Guid.NewGuid(),
                            recurringBill.Id,
                            recurringBill.FamilyId,
                            dueDate,
                            nowUtc,
                            transactionId: null,
                            result: "SkippedNoAccount",
                            notes: "No account found for family."),
                        cancellationToken);
                    continue;
                }

                try
                {
                    var transaction = await transactionService.CreateAsync(
                        accountId.Value,
                        -recurringBill.Amount.Amount,
                        $"Recurring bill auto-post: {recurringBill.Name}",
                        recurringBill.Merchant,
                        new DateTimeOffset(dueDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
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
                            dueDate,
                            nowUtc,
                            transaction.Id,
                            result: "Posted",
                            notes: null),
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    await executionRepository.AddAsync(
                        new RecurringBillExecution(
                            Guid.NewGuid(),
                            recurringBill.Id,
                            recurringBill.FamilyId,
                            dueDate,
                            nowUtc,
                            transactionId: null,
                            result: "Failed",
                            notes: Truncate(ex.Message, 500)),
                        cancellationToken);

                    logger.LogWarning(
                        ex,
                        "Recurring auto-post failed for bill {RecurringBillId} on {DueDate}.",
                        recurringBill.Id,
                        dueDate);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation and allow graceful shutdown.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Recurring auto-post worker loop failed.");
        }
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
