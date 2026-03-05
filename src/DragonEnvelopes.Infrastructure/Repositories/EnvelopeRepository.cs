using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class EnvelopeRepository(DragonEnvelopesDbContext dbContext) : IEnvelopeRepository
{
    public async Task AddEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        dbContext.Envelopes.Add(envelope);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Families
            .AsNoTracking()
            .AnyAsync(x => x.Id == familyId, cancellationToken);
    }

    public Task<bool> EnvelopeNameExistsAsync(
        Guid familyId,
        string name,
        Guid? excludeEnvelopeId = null,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Envelopes
            .AsNoTracking()
            .AnyAsync(
                x => x.FamilyId == familyId
                    && (!excludeEnvelopeId.HasValue || x.Id != excludeEnvelopeId.Value)
                    && EF.Functions.ILike(x.Name, name),
                cancellationToken);
    }

    public Task<Envelope?> GetByIdAsync(Guid envelopeId, CancellationToken cancellationToken = default)
    {
        return dbContext.Envelopes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == envelopeId, cancellationToken);
    }

    public Task<Envelope?> GetByIdForUpdateAsync(Guid envelopeId, CancellationToken cancellationToken = default)
    {
        return dbContext.Envelopes
            .FirstOrDefaultAsync(x => x.Id == envelopeId, cancellationToken);
    }

    public async Task<IReadOnlyList<Envelope>> ListByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Envelopes
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .OrderBy(x => x.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
