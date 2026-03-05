using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class BudgetRepository(DragonEnvelopesDbContext dbContext) : IBudgetRepository
{
    public async Task AddAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        dbContext.Budgets.Add(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Families
            .AsNoTracking()
            .AnyAsync(x => x.Id == familyId, cancellationToken);
    }

    public Task<bool> ExistsForMonthAsync(
        Guid familyId,
        BudgetMonth month,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Budgets
            .AsNoTracking()
            .AnyAsync(
                x => x.FamilyId == familyId && x.Month == month,
                cancellationToken);
    }

    public Task<Budget?> GetByFamilyAndMonthAsync(
        Guid familyId,
        BudgetMonth month,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Budgets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.Month == month,
                cancellationToken);
    }

    public Task<Budget?> GetByIdForUpdateAsync(Guid budgetId, CancellationToken cancellationToken = default)
    {
        return dbContext.Budgets
            .FirstOrDefaultAsync(x => x.Id == budgetId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
