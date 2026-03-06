namespace DragonEnvelopes.Desktop.Services;

public sealed record OnboardingProfileData(
    Guid Id,
    Guid FamilyId,
    bool AccountsCompleted,
    bool EnvelopesCompleted,
    bool BudgetCompleted,
    bool IsCompleted,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc);
