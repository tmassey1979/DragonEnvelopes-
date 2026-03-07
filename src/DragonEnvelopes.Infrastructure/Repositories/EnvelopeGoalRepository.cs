using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class EnvelopeGoalRepository(DragonEnvelopesDbContext dbContext) : IEnvelopeGoalRepository
{
    public Task AddAsync(EnvelopeGoal goal, CancellationToken cancellationToken = default)
    {
        dbContext.EnvelopeGoals.Add(goal);
        return Task.CompletedTask;
    }

    public Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Families
            .AsNoTracking()
            .AnyAsync(x => x.Id == familyId, cancellationToken);
    }

    public Task<bool> EnvelopeExistsAsync(
        Guid envelopeId,
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Envelopes
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == envelopeId && x.FamilyId == familyId,
                cancellationToken);
    }

    public Task<bool> ExistsForEnvelopeAsync(
        Guid envelopeId,
        Guid? excludeGoalId = null,
        CancellationToken cancellationToken = default)
    {
        return dbContext.EnvelopeGoals
            .AsNoTracking()
            .AnyAsync(
                x => x.EnvelopeId == envelopeId
                     && (!excludeGoalId.HasValue || x.Id != excludeGoalId.Value),
                cancellationToken);
    }

    public Task<EnvelopeGoal?> GetByIdAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        return dbContext.EnvelopeGoals
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == goalId, cancellationToken);
    }

    public Task<EnvelopeGoal?> GetByIdForUpdateAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        return dbContext.EnvelopeGoals
            .FirstOrDefaultAsync(x => x.Id == goalId, cancellationToken);
    }

    public async Task<IReadOnlyList<EnvelopeGoal>> ListByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.EnvelopeGoals
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .OrderBy(x => x.DueDate)
            .ToArrayAsync(cancellationToken);
    }

    public Task DeleteAsync(EnvelopeGoal goal, CancellationToken cancellationToken = default)
    {
        dbContext.EnvelopeGoals.Remove(goal);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
