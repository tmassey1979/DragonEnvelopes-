namespace DragonEnvelopes.Contracts.Families;

public sealed record RedeemFamilyInviteRequest(
    string InviteToken,
    string? MemberName,
    string? MemberEmail);
