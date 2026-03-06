namespace DragonEnvelopes.Application.DTOs;

public sealed record OnboardingMilestoneSignalsDetails(
    bool MembersCompleted,
    bool AccountsCompleted,
    bool EnvelopesCompleted,
    bool BudgetCompleted,
    bool PlaidCompleted,
    bool StripeAccountsCompleted,
    bool CardsCompleted,
    bool AutomationCompleted);
