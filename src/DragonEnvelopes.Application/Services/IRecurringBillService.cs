using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IRecurringBillService
{
    Task<RecurringBillDetails> CreateAsync(
        Guid familyId,
        string name,
        string merchant,
        decimal amount,
        string frequency,
        int dayOfMonth,
        DateOnly startDate,
        DateOnly? endDate,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecurringBillDetails>> ListByFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<RecurringBillDetails> UpdateAsync(
        Guid recurringBillId,
        string name,
        string merchant,
        decimal amount,
        string frequency,
        int dayOfMonth,
        DateOnly startDate,
        DateOnly? endDate,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid recurringBillId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecurringBillExecutionDetails>> ListExecutionsAsync(
        Guid recurringBillId,
        int take = 25,
        string? result = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecurringBillProjectionItemDetails>> ProjectAsync(
        Guid familyId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}
