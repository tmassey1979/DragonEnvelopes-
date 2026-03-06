namespace DragonEnvelopes.Contracts.Onboarding;

public sealed record OnboardingBootstrapRequest(
    IReadOnlyList<OnboardingBootstrapAccountRequest> Accounts,
    IReadOnlyList<OnboardingBootstrapEnvelopeRequest> Envelopes,
    OnboardingBootstrapBudgetRequest? Budget);
