namespace DragonEnvelopes.Desktop.Services;

public sealed record FamilyMemberImportPreviewResultData(
    int Parsed,
    int Valid,
    int Deduped,
    IReadOnlyList<FamilyMemberImportPreviewRowData> Rows);

public sealed record FamilyMemberImportPreviewRowData(
    int RowNumber,
    string KeycloakUserId,
    string Name,
    string Email,
    string Role,
    bool IsDuplicate,
    string Errors);

public sealed record FamilyMemberImportCommitResultData(
    int Parsed,
    int Valid,
    int Deduped,
    int Inserted,
    int Failed);
