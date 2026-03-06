using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IOnboardingProfileService
{
    Task<OnboardingProfileDetails> GetOrCreateAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<OnboardingProfileDetails> UpdateAsync(
        Guid familyId,
        bool membersCompleted,
        bool accountsCompleted,
        bool envelopesCompleted,
        bool budgetCompleted,
        bool plaidCompleted,
        bool stripeAccountsCompleted,
        bool cardsCompleted,
        bool automationCompleted,
        CancellationToken cancellationToken = default);
}
