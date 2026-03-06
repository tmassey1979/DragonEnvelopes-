namespace DragonEnvelopes.Contracts.Onboarding;

public sealed record OnboardingProfileResponse(
    Guid Id,
    Guid FamilyId,
    bool AccountsCompleted,
    bool EnvelopesCompleted,
    bool BudgetCompleted,
    bool IsCompleted,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc);
