using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public interface IRecurringBillsDataService
{
    Task<IReadOnlyList<RecurringBillItemViewModel>> GetBillsAsync(CancellationToken cancellationToken = default);

    Task<RecurringBillItemViewModel> CreateBillAsync(
        string name,
        string merchant,
        decimal amount,
        string frequency,
        int dayOfMonth,
        DateOnly startDate,
        DateOnly? endDate,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<RecurringBillItemViewModel> UpdateBillAsync(
        Guid id,
        string name,
        string merchant,
        decimal amount,
        string frequency,
        int dayOfMonth,
        DateOnly startDate,
        DateOnly? endDate,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task DeleteBillAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecurringBillProjectionItemViewModel>> GetProjectionAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecurringBillExecutionItemViewModel>> GetExecutionHistoryAsync(
        Guid recurringBillId,
        int take = 25,
        string? result = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    Task<RecurringAutoPostRunResultViewModel> RunAutoPostAsync(
        DateOnly? dueDate = null,
        CancellationToken cancellationToken = default);
}
