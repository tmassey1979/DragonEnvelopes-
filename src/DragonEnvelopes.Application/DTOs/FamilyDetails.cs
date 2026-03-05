namespace DragonEnvelopes.Application.DTOs;

public sealed record FamilyDetails(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    IReadOnlyList<FamilyMemberDetails> Members);

public sealed record FamilyMemberDetails(
    Guid Id,
    Guid FamilyId,
    string KeycloakUserId,
    string Name,
    string Email,
    string Role);
