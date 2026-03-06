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

        var profile = await GetOrCreateEntityAsync(familyId, cancellationToken);

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

        var profile = await GetOrCreateEntityAsync(familyId, cancellationToken);
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

    public async Task<OnboardingProfileDetails> ReconcileAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        if (!await onboardingProfileRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        var profile = await GetOrCreateEntityAsync(familyId, cancellationToken);
        var signals = await onboardingProfileRepository.GetMilestoneSignalsAsync(familyId, cancellationToken);

        profile.UpdateMilestones(
            signals.MembersCompleted,
            signals.AccountsCompleted,
            signals.EnvelopesCompleted,
            signals.BudgetCompleted,
            signals.PlaidCompleted,
            signals.StripeAccountsCompleted,
            signals.CardsCompleted,
            signals.AutomationCompleted,
            clock.UtcNow);

        await onboardingProfileRepository.SaveChangesAsync(cancellationToken);
        return Map(profile);
    }

    private async Task<OnboardingProfile> GetOrCreateEntityAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var profile = await onboardingProfileRepository.GetByFamilyIdForUpdateAsync(familyId, cancellationToken);
        if (profile is not null)
        {
            return profile;
        }

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
        return profile;
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
