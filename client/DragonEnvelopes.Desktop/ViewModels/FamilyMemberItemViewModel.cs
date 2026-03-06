namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record FamilyMemberItemViewModel(
    Guid Id,
    string KeycloakUserId,
    string Name,
    string Email,
    string Role);
