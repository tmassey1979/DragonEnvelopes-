namespace DragonEnvelopes.Contracts.Families;

public sealed record RegisterFamilyInviteAccountRequest(
    string InviteToken,
    string FirstName,
    string LastName,
    string Email,
    string Password);
