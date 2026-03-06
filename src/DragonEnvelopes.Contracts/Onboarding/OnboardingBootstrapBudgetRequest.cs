namespace DragonEnvelopes.Contracts.Onboarding;

public sealed record OnboardingBootstrapBudgetRequest(
    string Month,
    decimal TotalIncome);
