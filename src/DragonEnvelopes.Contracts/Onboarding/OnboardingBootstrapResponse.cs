namespace DragonEnvelopes.Contracts.Onboarding;

public sealed record OnboardingBootstrapResponse(
    Guid FamilyId,
    int AccountsCreated,
    int EnvelopesCreated,
    bool BudgetCreated);
