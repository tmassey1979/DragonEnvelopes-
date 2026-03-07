using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class EnvelopeFinancialAccountRepository(DragonEnvelopesDbContext dbContext) : IEnvelopeFinancialAccountRepository
{
    public Task<EnvelopeFinancialAccount?> GetByEnvelopeIdAsync(Guid envelopeId, CancellationToken cancellationToken = default)
    {
        return dbContext.EnvelopeFinancialAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EnvelopeId == envelopeId, cancellationToken);
    }

    public Task<EnvelopeFinancialAccount?> GetByEnvelopeIdForUpdateAsync(Guid envelopeId, CancellationToken cancellationToken = default)
    {
        return dbContext.EnvelopeFinancialAccounts
            .FirstOrDefaultAsync(x => x.EnvelopeId == envelopeId, cancellationToken);
    }

    public async Task<IReadOnlyList<EnvelopeFinancialAccount>> ListByFamilyAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.EnvelopeFinancialAccounts
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public Task AddAsync(EnvelopeFinancialAccount account, CancellationToken cancellationToken = default)
    {
        dbContext.EnvelopeFinancialAccounts.Add(account);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
