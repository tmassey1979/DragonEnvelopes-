using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IRecurringBillExecutionRepository
{
    Task<bool> HasExecutionAsync(
        Guid recurringBillId,
        DateOnly dueDate,
        CancellationToken cancellationToken = default);

    Task AddAsync(RecurringBillExecution execution, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecurringBillExecution>> ListByRecurringBillAsync(
        Guid recurringBillId,
        CancellationToken cancellationToken = default);
}
