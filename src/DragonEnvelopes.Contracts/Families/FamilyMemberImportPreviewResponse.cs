namespace DragonEnvelopes.Contracts.Families;

public sealed record FamilyMemberImportPreviewResponse(
    int Parsed,
    int Valid,
    int Deduped,
    IReadOnlyList<FamilyMemberImportPreviewRowResponse> Rows);

public sealed record FamilyMemberImportPreviewRowResponse(
    int RowNumber,
    string? KeycloakUserId,
    string? Name,
    string? Email,
    string? Role,
    bool IsDuplicate,
    IReadOnlyList<string> Errors);
