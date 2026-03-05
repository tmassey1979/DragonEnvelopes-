namespace DragonEnvelopes.Contracts.Families;

public sealed record CompleteFamilyOnboardingRequest(
    string FamilyName,
    string PrimaryGuardianName,
    string Email,
    string Password);
