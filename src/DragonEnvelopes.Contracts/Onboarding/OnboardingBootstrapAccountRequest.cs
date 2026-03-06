namespace DragonEnvelopes.Contracts.Onboarding;

public sealed record OnboardingBootstrapAccountRequest(
    string Name,
    string Type,
    decimal OpeningBalance);
