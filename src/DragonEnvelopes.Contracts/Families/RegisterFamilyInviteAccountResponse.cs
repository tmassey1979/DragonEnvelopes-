namespace DragonEnvelopes.Contracts.Families;

public sealed record RegisterFamilyInviteAccountResponse(
    FamilyInviteResponse Invite,
    FamilyMemberResponse Member,
    bool CreatedNewMember);
