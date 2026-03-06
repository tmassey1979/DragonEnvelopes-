namespace DragonEnvelopes.Desktop.Services;

public sealed record OnboardingProfileData(
    Guid Id,
    Guid FamilyId,
    bool MembersCompleted,
    bool AccountsCompleted,
    bool EnvelopesCompleted,
    bool BudgetCompleted,
    bool PlaidCompleted,
    bool StripeAccountsCompleted,
    bool CardsCompleted,
    bool AutomationCompleted,
    bool IsCompleted,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc);
