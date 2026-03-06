namespace DragonEnvelopes.Contracts.Families;

public sealed record CreateFamilyInviteResponse(
    FamilyInviteResponse Invite,
    string InviteToken);
