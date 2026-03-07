namespace DragonEnvelopes.Application.DTOs;

public sealed record FamilyInviteRedemptionDetails(
    FamilyInviteDetails Invite,
    FamilyMemberDetails Member,
    bool CreatedNewMember);
