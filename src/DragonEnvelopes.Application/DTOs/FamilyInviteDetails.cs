namespace DragonEnvelopes.Application.DTOs;

public sealed record FamilyInviteDetails(
    Guid Id,
    Guid FamilyId,
    string Email,
    string Role,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? CancelledAtUtc);
