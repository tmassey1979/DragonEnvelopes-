using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class RecurringBillRepository(DragonEnvelopesDbContext dbContext) : IRecurringBillRepository
{
    public async Task AddAsync(RecurringBill recurringBill, CancellationToken cancellationToken = default)
    {
        dbContext.RecurringBills.Add(recurringBill);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Families
            .AsNoTracking()
            .AnyAsync(x => x.Id == familyId, cancellationToken);
    }

    public async Task<IReadOnlyList<RecurringBill>> ListByFamilyAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.RecurringBills
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .OrderBy(x => x.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<RecurringBill?> GetByIdForUpdateAsync(Guid recurringBillId, CancellationToken cancellationToken = default)
    {
        return dbContext.RecurringBills
            .FirstOrDefaultAsync(x => x.Id == recurringBillId, cancellationToken);
    }

    public async Task DeleteAsync(RecurringBill recurringBill, CancellationToken cancellationToken = default)
    {
        dbContext.RecurringBills.Remove(recurringBill);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
