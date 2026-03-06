using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class AccountRepository(DragonEnvelopesDbContext dbContext) : IAccountRepository
{
    public async Task AddAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Families
            .AsNoTracking()
            .AnyAsync(x => x.Id == familyId, cancellationToken);
    }

    public Task<bool> AccountNameExistsAsync(
        Guid familyId,
        string name,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Accounts
            .AsNoTracking()
            .AnyAsync(
                x => x.FamilyId == familyId
                    && EF.Functions.ILike(x.Name, name),
                cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> ListAccountsAsync(
        Guid? familyId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Accounts.AsNoTracking();
        if (familyId.HasValue)
        {
            query = query.Where(x => x.FamilyId == familyId.Value);
        }

        return await query
            .OrderBy(x => x.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Account?> GetByIdForUpdateAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Accounts
            .FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
