namespace DragonEnvelopes.Application.DTOs;

public sealed record ImportPreviewDetails(
    int Parsed,
    int Valid,
    int Deduped,
    IReadOnlyList<ImportPreviewRowDetails> Rows);

public sealed record ImportPreviewRowDetails(
    int RowNumber,
    DateOnly? OccurredOn,
    decimal? Amount,
    string? Merchant,
    string? Description,
    string? Category,
    bool IsDuplicate,
    IReadOnlyList<string> Errors);

public sealed record ImportCommitDetails(
    int Parsed,
    int Valid,
    int Deduped,
    int Inserted,
    int Failed);
