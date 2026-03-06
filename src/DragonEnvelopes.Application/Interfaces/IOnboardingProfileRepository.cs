using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IOnboardingProfileRepository
{
    Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<OnboardingProfile?> GetByFamilyIdForUpdateAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<OnboardingMilestoneSignalsDetails> GetMilestoneSignalsAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task AddAsync(OnboardingProfile profile, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
