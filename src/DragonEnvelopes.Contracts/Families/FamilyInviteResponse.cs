namespace DragonEnvelopes.Contracts.Families;

public sealed record FamilyInviteResponse(
    Guid Id,
    Guid FamilyId,
    string Email,
    string Role,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? CancelledAtUtc);
