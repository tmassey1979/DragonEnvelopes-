using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public sealed record CreateFamilyInviteResultData(
    FamilyInviteItemViewModel Invite,
    string InviteToken);
