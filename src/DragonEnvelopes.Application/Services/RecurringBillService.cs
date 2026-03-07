using System.Diagnostics;
using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class RecurringBillService(
    IRecurringBillRepository recurringBillRepository,
    IRecurringBillExecutionRepository recurringBillExecutionRepository,
    IIntegrationOutboxRepository? integrationOutboxRepository = null) : IRecurringBillService
{
    public async Task<RecurringBillDetails> CreateAsync(
        Guid familyId,
        string name,
        string merchant,
        decimal amount,
        string frequency,
        int dayOfMonth,
        DateOnly startDate,
        DateOnly? endDate,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (!await recurringBillRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        var parsedFrequency = ParseFrequency(frequency);
        var recurringBill = new RecurringBill(
            Guid.NewGuid(),
            familyId,
            name,
            merchant,
            Money.FromDecimal(amount),
            parsedFrequency,
            dayOfMonth,
            startDate,
            endDate,
            isActive);

        await recurringBillRepository.AddAsync(recurringBill, cancellationToken);
        var created = Map(recurringBill);
        var now = DateTimeOffset.UtcNow;
        await EnqueuePlanningOutboxAsync(
            recurringBill.FamilyId,
            IntegrationEventRoutingKeys.PlanningRecurringBillCreatedV1,
            PlanningIntegrationEventNames.RecurringBillCreated,
            new RecurringBillCreatedIntegrationEvent(
                Guid.NewGuid(),
                now,
                recurringBill.FamilyId,
                ResolveCorrelationId(),
                recurringBill.Id,
                recurringBill.Name,
                recurringBill.Merchant,
                recurringBill.Amount.Amount,
                recurringBill.Frequency.ToString(),
                recurringBill.DayOfMonth,
                recurringBill.StartDate,
                recurringBill.EndDate,
                recurringBill.IsActive),
            now,
            cancellationToken);
        await recurringBillRepository.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<IReadOnlyList<RecurringBillDetails>> ListByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        var recurringBills = await recurringBillRepository.ListByFamilyAsync(familyId, cancellationToken);
        return recurringBills.Select(Map).ToArray();
    }

    public async Task<RecurringBillDetails> UpdateAsync(
        Guid recurringBillId,
        string name,
        string merchant,
        decimal amount,
        string frequency,
        int dayOfMonth,
        DateOnly startDate,
        DateOnly? endDate,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var recurringBill = await recurringBillRepository.GetByIdForUpdateAsync(recurringBillId, cancellationToken);
        if (recurringBill is null)
        {
            throw new DomainValidationException("Recurring bill was not found.");
        }

        recurringBill.Update(
            name,
            merchant,
            Money.FromDecimal(amount),
            ParseFrequency(frequency),
            dayOfMonth,
            startDate,
            endDate,
            isActive);
        var now = DateTimeOffset.UtcNow;
        await EnqueuePlanningOutboxAsync(
            recurringBill.FamilyId,
            IntegrationEventRoutingKeys.PlanningRecurringBillUpdatedV1,
            PlanningIntegrationEventNames.RecurringBillUpdated,
            new RecurringBillUpdatedIntegrationEvent(
                Guid.NewGuid(),
                now,
                recurringBill.FamilyId,
                ResolveCorrelationId(),
                recurringBill.Id,
                recurringBill.Name,
                recurringBill.Merchant,
                recurringBill.Amount.Amount,
                recurringBill.Frequency.ToString(),
                recurringBill.DayOfMonth,
                recurringBill.StartDate,
                recurringBill.EndDate,
                recurringBill.IsActive),
            now,
            cancellationToken);
        await recurringBillRepository.SaveChangesAsync(cancellationToken);
        return Map(recurringBill);
    }

    public async Task DeleteAsync(Guid recurringBillId, CancellationToken cancellationToken = default)
    {
        var recurringBill = await recurringBillRepository.GetByIdForUpdateAsync(recurringBillId, cancellationToken);
        if (recurringBill is null)
        {
            throw new DomainValidationException("Recurring bill was not found.");
        }

        await recurringBillRepository.DeleteAsync(recurringBill, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        await EnqueuePlanningOutboxAsync(
            recurringBill.FamilyId,
            IntegrationEventRoutingKeys.PlanningRecurringBillDeletedV1,
            PlanningIntegrationEventNames.RecurringBillDeleted,
            new RecurringBillDeletedIntegrationEvent(
                Guid.NewGuid(),
                now,
                recurringBill.FamilyId,
                ResolveCorrelationId(),
                recurringBill.Id,
                recurringBill.Name,
                recurringBill.Merchant,
                recurringBill.Amount.Amount,
                recurringBill.Frequency.ToString()),
            now,
            cancellationToken);
        await recurringBillRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecurringBillExecutionDetails>> ListExecutionsAsync(
        Guid recurringBillId,
        int take = 25,
        string? result = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var recurringBill = await recurringBillRepository.GetByIdForUpdateAsync(recurringBillId, cancellationToken);
        if (recurringBill is null)
        {
            throw new DomainValidationException("Recurring bill was not found.");
        }

        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            throw new DomainValidationException("'fromDate' must be earlier than or equal to 'toDate'.");
        }

        var normalizedTake = Math.Clamp(take, 1, 100);
        var normalizedResult = string.IsNullOrWhiteSpace(result)
            ? null
            : result.Trim();
        var executions = await recurringBillExecutionRepository.ListByRecurringBillAsync(recurringBillId, cancellationToken);
        var filtered = executions.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(normalizedResult))
        {
            filtered = filtered.Where(execution =>
                string.Equals(execution.Result, normalizedResult, StringComparison.OrdinalIgnoreCase));
        }

        if (fromDate.HasValue)
        {
            filtered = filtered.Where(execution =>
                DateOnly.FromDateTime(execution.ExecutedAtUtc.UtcDateTime) >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            filtered = filtered.Where(execution =>
                DateOnly.FromDateTime(execution.ExecutedAtUtc.UtcDateTime) <= toDate.Value);
        }

        return filtered
            .Take(normalizedTake)
            .Select(MapExecution)
            .ToArray();
    }

    public async Task<IReadOnlyList<RecurringBillProjectionItemDetails>> ProjectAsync(
        Guid familyId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new DomainValidationException("'from' must be earlier than or equal to 'to'.");
        }

        var recurringBills = await recurringBillRepository.ListByFamilyAsync(familyId, cancellationToken);
        var projection = recurringBills
            .Where(static bill => bill.IsActive)
            .SelectMany(bill => ProjectBill(bill, from, to))
            .OrderBy(static item => item.DueDate)
            .ThenBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return projection;
    }

    private static IEnumerable<RecurringBillProjectionItemDetails> ProjectBill(
        RecurringBill recurringBill,
        DateOnly from,
        DateOnly to)
    {
        var effectiveFrom = Max(from, recurringBill.StartDate);
        var effectiveTo = recurringBill.EndDate.HasValue
            ? Min(to, recurringBill.EndDate.Value)
            : to;
        if (effectiveFrom > effectiveTo)
        {
            yield break;
        }

        switch (recurringBill.Frequency)
        {
            case RecurringBillFrequency.Monthly:
            {
                var cursor = new DateOnly(effectiveFrom.Year, effectiveFrom.Month, 1);
                while (cursor <= effectiveTo)
                {
                    var day = Math.Min(recurringBill.DayOfMonth, DateTime.DaysInMonth(cursor.Year, cursor.Month));
                    var dueDate = new DateOnly(cursor.Year, cursor.Month, day);
                    if (dueDate >= effectiveFrom && dueDate <= effectiveTo)
                    {
                        yield return new RecurringBillProjectionItemDetails(
                            recurringBill.Id,
                            recurringBill.Name,
                            recurringBill.Merchant,
                            recurringBill.Amount.Amount,
                            dueDate);
                    }

                    cursor = cursor.AddMonths(1);
                }

                yield break;
            }

            case RecurringBillFrequency.Weekly:
                foreach (var dueDate in ProjectByDayInterval(recurringBill, effectiveFrom, effectiveTo, 7))
                {
                    yield return dueDate;
                }

                yield break;

            case RecurringBillFrequency.BiWeekly:
                foreach (var dueDate in ProjectByDayInterval(recurringBill, effectiveFrom, effectiveTo, 14))
                {
                    yield return dueDate;
                }

                yield break;
            default:
                yield break;
        }
    }

    private static IEnumerable<RecurringBillProjectionItemDetails> ProjectByDayInterval(
        RecurringBill recurringBill,
        DateOnly from,
        DateOnly to,
        int intervalDays)
    {
        var dueDate = recurringBill.StartDate;
        while (dueDate < from)
        {
            dueDate = dueDate.AddDays(intervalDays);
        }

        while (dueDate <= to)
        {
            yield return new RecurringBillProjectionItemDetails(
                recurringBill.Id,
                recurringBill.Name,
                recurringBill.Merchant,
                recurringBill.Amount.Amount,
                dueDate);
            dueDate = dueDate.AddDays(intervalDays);
        }
    }

    private static RecurringBillFrequency ParseFrequency(string frequency)
    {
        if (!Enum.TryParse<RecurringBillFrequency>(frequency, true, out var parsed))
        {
            throw new DomainValidationException("Recurring bill frequency is invalid.");
        }

        return parsed;
    }

    private static RecurringBillDetails Map(RecurringBill recurringBill)
    {
        return new RecurringBillDetails(
            recurringBill.Id,
            recurringBill.FamilyId,
            recurringBill.Name,
            recurringBill.Merchant,
            recurringBill.Amount.Amount,
            recurringBill.Frequency.ToString(),
            recurringBill.DayOfMonth,
            recurringBill.StartDate,
            recurringBill.EndDate,
            recurringBill.IsActive);
    }

    private static RecurringBillExecutionDetails MapExecution(RecurringBillExecution execution)
    {
        var idempotencyKey = $"{execution.RecurringBillId:N}:{execution.DueDate:yyyy-MM-dd}";
        return new RecurringBillExecutionDetails(
            execution.Id,
            execution.RecurringBillId,
            execution.FamilyId,
            execution.DueDate,
            execution.ExecutedAtUtc,
            execution.TransactionId,
            execution.Result,
            TrimNotes(execution.Notes),
            idempotencyKey);
    }

    private static string? TrimNotes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= 240
            ? normalized
            : $"{normalized[..240]}...";
    }

    private static DateOnly Max(DateOnly left, DateOnly right)
    {
        return left >= right ? left : right;
    }

    private static DateOnly Min(DateOnly left, DateOnly right)
    {
        return left <= right ? left : right;
    }

    private static string ResolveCorrelationId()
    {
        return Activity.Current?.Id ?? Guid.NewGuid().ToString("D");
    }

    private Task EnqueuePlanningOutboxAsync<TPayload>(
        Guid familyId,
        string routingKey,
        string eventName,
        TPayload payload,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        return IntegrationOutboxEnqueuer.EnqueueAsync(
            integrationOutboxRepository,
            familyId,
            IntegrationEventSourceServices.PlanningApi,
            routingKey,
            eventName,
            payload,
            createdAtUtc,
            cancellationToken);
    }
}
