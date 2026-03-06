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
                accountsCompleted: false,
                envelopesCompleted: false,
                budgetCompleted: false,
                createdAtUtc: now,
                updatedAtUtc: now,
                completedAtUtc: null);
            await onboardingProfileRepository.AddAsync(profile, cancellationToken);
        }

        return Map(profile);
    }

    public async Task<OnboardingProfileDetails> UpdateAsync(
        Guid familyId,
        bool accountsCompleted,
        bool envelopesCompleted,
        bool budgetCompleted,
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
                accountsCompleted,
                envelopesCompleted,
                budgetCompleted,
                now,
                now,
                completedAtUtc: accountsCompleted && envelopesCompleted && budgetCompleted ? now : null);
            await onboardingProfileRepository.AddAsync(profile, cancellationToken);
            return Map(profile);
        }

        profile.UpdateMilestones(accountsCompleted, envelopesCompleted, budgetCompleted, clock.UtcNow);
        await onboardingProfileRepository.SaveChangesAsync(cancellationToken);
        return Map(profile);
    }

    private static OnboardingProfileDetails Map(OnboardingProfile profile)
    {
        return new OnboardingProfileDetails(
            profile.Id,
            profile.FamilyId,
            profile.AccountsCompleted,
            profile.EnvelopesCompleted,
            profile.BudgetCompleted,
            profile.IsCompleted,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            profile.CompletedAtUtc);
    }
}
