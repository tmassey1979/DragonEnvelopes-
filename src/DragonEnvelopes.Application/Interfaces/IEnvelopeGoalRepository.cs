using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IEnvelopeGoalRepository
{
    Task AddAsync(EnvelopeGoal goal, CancellationToken cancellationToken = default);

    Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<bool> EnvelopeExistsAsync(Guid envelopeId, Guid familyId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForEnvelopeAsync(
        Guid envelopeId,
        Guid? excludeGoalId = null,
        CancellationToken cancellationToken = default);

    Task<EnvelopeGoal?> GetByIdAsync(Guid goalId, CancellationToken cancellationToken = default);

    Task<EnvelopeGoal?> GetByIdForUpdateAsync(Guid goalId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvelopeGoal>> ListByFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task DeleteAsync(EnvelopeGoal goal, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
