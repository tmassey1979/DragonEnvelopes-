namespace DragonEnvelopes.Application.DTOs;

public sealed record FamilyMemberImportPreviewDetails(
    int Parsed,
    int Valid,
    int Deduped,
    IReadOnlyList<FamilyMemberImportPreviewRowDetails> Rows);

public sealed record FamilyMemberImportPreviewRowDetails(
    int RowNumber,
    string? KeycloakUserId,
    string? Name,
    string? Email,
    string? Role,
    bool IsDuplicate,
    IReadOnlyList<string> Errors);

public sealed record FamilyMemberImportCommitDetails(
    int Parsed,
    int Valid,
    int Deduped,
    int Inserted,
    int Failed);
