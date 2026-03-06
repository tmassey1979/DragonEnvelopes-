namespace DragonEnvelopes.Contracts.Families;

public sealed record CreateFamilyInviteRequest(
    string Email,
    string Role,
    int ExpiresInHours = 168);
