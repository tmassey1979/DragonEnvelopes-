namespace DragonEnvelopes.Contracts.Families;

public sealed record RedeemFamilyInviteResponse(
    FamilyInviteResponse Invite,
    FamilyMemberResponse Member,
    bool CreatedNewMember);
