using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IOnboardingProfileService
{
    Task<OnboardingProfileDetails> GetOrCreateAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<OnboardingProfileDetails> UpdateAsync(
        Guid familyId,
        bool accountsCompleted,
        bool envelopesCompleted,
        bool budgetCompleted,
        CancellationToken cancellationToken = default);
}
