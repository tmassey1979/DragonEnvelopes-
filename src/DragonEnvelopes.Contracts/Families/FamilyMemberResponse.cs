namespace DragonEnvelopes.Contracts.Families;

public sealed record FamilyMemberResponse(
    Guid Id,
    Guid FamilyId,
    string KeycloakUserId,
    string Name,
    string Email,
    string Role);

