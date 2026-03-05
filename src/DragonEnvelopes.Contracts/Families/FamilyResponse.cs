namespace DragonEnvelopes.Contracts.Families;

public sealed record FamilyResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    IReadOnlyList<FamilyMemberResponse> Members);

