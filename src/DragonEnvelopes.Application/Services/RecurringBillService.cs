using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class RecurringBillService(
    IRecurringBillRepository recurringBillRepository) : IRecurringBillService
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
        return Map(recurringBill);
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

    private static DateOnly Max(DateOnly left, DateOnly right)
    {
        return left >= right ? left : right;
    }

    private static DateOnly Min(DateOnly left, DateOnly right)
    {
        return left <= right ? left : right;
    }
}
