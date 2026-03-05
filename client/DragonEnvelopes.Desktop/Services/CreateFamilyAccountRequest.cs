namespace DragonEnvelopes.Desktop.Services;

public sealed record CreateFamilyAccountRequest(
    string FamilyName,
    string PrimaryGuardianFirstName,
    string PrimaryGuardianLastName,
    string Email,
    string Password);
