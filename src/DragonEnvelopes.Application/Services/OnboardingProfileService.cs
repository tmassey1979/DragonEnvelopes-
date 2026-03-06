using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Services;

public sealed class OnboardingProfileService(
    IOnboardingProfileRepository onboardingProfileRepository,
    IClock clock) : IOnboardingProfileService
{
    public async Task<OnboardingProfileDetails> GetOrCreateAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        if (!await onboardingProfileRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        var profile = await onboardingProfileRepository.GetByFamilyIdForUpdateAsync(familyId, cancellationToken);
        if (profile is null)
        {
            var now = clock.UtcNow;
            profile = new OnboardingProfile(
                Guid.NewGuid(),
                familyId,
                membersCompleted: false,
                accountsCompleted: false,
                envelopesCompleted: false,
                budgetCompleted: false,
                plaidCompleted: false,
                stripeAccountsCompleted: false,
                cardsCompleted: false,
                automationCompleted: false,
                createdAtUtc: now,
                updatedAtUtc: now,
                completedAtUtc: null);
            await onboardingProfileRepository.AddAsync(profile, cancellationToken);
        }

        return Map(profile);
    }

    public async Task<OnboardingProfileDetails> UpdateAsync(
        Guid familyId,
        bool membersCompleted,
        bool accountsCompleted,
        bool envelopesCompleted,
        bool budgetCompleted,
        bool plaidCompleted,
        bool stripeAccountsCompleted,
        bool cardsCompleted,
        bool automationCompleted,
        CancellationToken cancellationToken = default)
    {
        if (!await onboardingProfileRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        var profile = await onboardingProfileRepository.GetByFamilyIdForUpdateAsync(familyId, cancellationToken);
        if (profile is null)
        {
            var now = clock.UtcNow;
            profile = new OnboardingProfile(
                Guid.NewGuid(),
                familyId,
                membersCompleted,
                accountsCompleted,
                envelopesCompleted,
                budgetCompleted,
                plaidCompleted,
                stripeAccountsCompleted,
                cardsCompleted,
                automationCompleted,
                now,
                now,
                completedAtUtc: membersCompleted
                                && accountsCompleted
                                && envelopesCompleted
                                && budgetCompleted
                                && plaidCompleted
                                && stripeAccountsCompleted
                                && cardsCompleted
                                && automationCompleted
                    ? now
                    : null);
            await onboardingProfileRepository.AddAsync(profile, cancellationToken);
            return Map(profile);
        }

        profile.UpdateMilestones(
            membersCompleted,
            accountsCompleted,
            envelopesCompleted,
            budgetCompleted,
            plaidCompleted,
            stripeAccountsCompleted,
            cardsCompleted,
            automationCompleted,
            clock.UtcNow);
        await onboardingProfileRepository.SaveChangesAsync(cancellationToken);
        return Map(profile);
    }

    private static OnboardingProfileDetails Map(OnboardingProfile profile)
    {
        return new OnboardingProfileDetails(
            profile.Id,
            profile.FamilyId,
            profile.MembersCompleted,
            profile.AccountsCompleted,
            profile.EnvelopesCompleted,
            profile.BudgetCompleted,
            profile.PlaidCompleted,
            profile.StripeAccountsCompleted,
            profile.CardsCompleted,
            profile.AutomationCompleted,
            profile.IsCompleted,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            profile.CompletedAtUtc);
    }
}
