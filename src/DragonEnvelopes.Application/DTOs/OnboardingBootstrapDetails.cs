namespace DragonEnvelopes.Application.DTOs;

public sealed record OnboardingBootstrapDetails(
    Guid FamilyId,
    int AccountsCreated,
    int EnvelopesCreated,
    bool BudgetCreated);
