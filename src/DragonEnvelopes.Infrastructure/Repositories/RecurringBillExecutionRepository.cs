using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class RecurringBillExecutionRepository(DragonEnvelopesDbContext dbContext) : IRecurringBillExecutionRepository
{
    public Task<bool> HasExecutionAsync(
        Guid recurringBillId,
        DateOnly dueDate,
        CancellationToken cancellationToken = default)
    {
        return dbContext.RecurringBillExecutions
            .AsNoTracking()
            .AnyAsync(
                x => x.RecurringBillId == recurringBillId
                     && x.DueDate == dueDate,
                cancellationToken);
    }

    public async Task AddAsync(RecurringBillExecution execution, CancellationToken cancellationToken = default)
    {
        dbContext.RecurringBillExecutions.Add(execution);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecurringBillExecution>> ListByRecurringBillAsync(
        Guid recurringBillId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.RecurringBillExecutions
            .AsNoTracking()
            .Where(x => x.RecurringBillId == recurringBillId)
            .OrderByDescending(x => x.DueDate)
            .ThenByDescending(x => x.ExecutedAtUtc)
            .ToArrayAsync(cancellationToken);
    }
}
