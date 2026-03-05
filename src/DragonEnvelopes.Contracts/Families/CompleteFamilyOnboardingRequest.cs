namespace DragonEnvelopes.Contracts.Families;

public sealed record CompleteFamilyOnboardingRequest(
    string FamilyName,
    string PrimaryGuardianFirstName,
    string PrimaryGuardianLastName,
    string Email,
    string Password);
