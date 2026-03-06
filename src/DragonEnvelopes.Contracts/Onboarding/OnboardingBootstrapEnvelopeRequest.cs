namespace DragonEnvelopes.Contracts.Onboarding;

public sealed record OnboardingBootstrapEnvelopeRequest(
    string Name,
    decimal MonthlyBudget);
