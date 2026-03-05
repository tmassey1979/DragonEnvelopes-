namespace DragonEnvelopes.Contracts.Families;

public sealed record AddFamilyMemberRequest(
    string KeycloakUserId,
    string Name,
    string Email,
    string Role);

