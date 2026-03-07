namespace DragonEnvelopes.Desktop.Services;

public sealed record FamilyInviteRedemptionResult(
    bool Succeeded,
    string Message,
    Guid? FamilyId = null,
    bool CreatedNewMember = false);
