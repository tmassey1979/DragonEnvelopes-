namespace DragonEnvelopes.Desktop.Services;

public sealed record CreateFamilyAccountRequest(
    string FamilyName,
    string PrimaryGuardianName,
    string Email,
    string Password);
