namespace DragonEnvelopes.Contracts.Onboarding;

public sealed record UpdateOnboardingProfileRequest(
    bool MembersCompleted,
    bool AccountsCompleted,
    bool EnvelopesCompleted,
    bool BudgetCompleted,
    bool PlaidCompleted,
    bool StripeAccountsCompleted,
    bool CardsCompleted,
    bool AutomationCompleted);
