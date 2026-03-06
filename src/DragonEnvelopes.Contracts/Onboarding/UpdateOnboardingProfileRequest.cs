namespace DragonEnvelopes.Contracts.Onboarding;

public sealed record UpdateOnboardingProfileRequest(
    bool AccountsCompleted,
    bool EnvelopesCompleted,
    bool BudgetCompleted);
