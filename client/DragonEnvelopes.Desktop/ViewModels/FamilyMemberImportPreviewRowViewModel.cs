namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record FamilyMemberImportPreviewRowViewModel(
    int RowNumber,
    string KeycloakUserId,
    string Name,
    string Email,
    string Role,
    bool IsDuplicate,
    string Errors);
