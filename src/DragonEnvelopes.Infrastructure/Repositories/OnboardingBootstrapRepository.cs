using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class OnboardingBootstrapRepository(DragonEnvelopesDbContext dbContext) : IOnboardingBootstrapRepository
{
    public Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Families
            .AsNoTracking()
            .AnyAsync(x => x.Id == familyId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> ListAccountNamesAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .Select(x => x.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> ListEnvelopeNamesAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Envelopes
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .Select(x => x.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> BudgetExistsAsync(Guid familyId, BudgetMonth month, CancellationToken cancellationToken = default)
    {
        return dbContext.Budgets
            .AsNoTracking()
            .AnyAsync(x => x.FamilyId == familyId && x.Month == month, cancellationToken);
    }

    public async Task SaveBootstrapAsync(
        IReadOnlyList<Account> accounts,
        IReadOnlyList<Envelope> envelopes,
        Budget? budget,
        CancellationToken cancellationToken = default)
    {
        if (accounts.Count > 0)
        {
            dbContext.Accounts.AddRange(accounts);
        }

        if (envelopes.Count > 0)
        {
            dbContext.Envelopes.AddRange(envelopes);
        }

        if (budget is not null)
        {
            dbContext.Budgets.Add(budget);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
