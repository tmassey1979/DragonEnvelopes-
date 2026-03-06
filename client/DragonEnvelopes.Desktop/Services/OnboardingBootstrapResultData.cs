namespace DragonEnvelopes.Desktop.Services;

public sealed record OnboardingBootstrapResultData(
    Guid FamilyId,
    int AccountsCreated,
    int EnvelopesCreated,
    bool BudgetCreated);
