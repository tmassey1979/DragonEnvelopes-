namespace DragonEnvelopes.Desktop.Services;

public sealed record RegisterFamilyInviteAccountRequestData(
    string InviteToken,
    string FirstName,
    string LastName,
    string Email,
    string Password);
