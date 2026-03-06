namespace DragonEnvelopes.Application.DTOs;

public sealed record CreateFamilyInviteResult(
    FamilyInviteDetails Invite,
    string InviteToken);
