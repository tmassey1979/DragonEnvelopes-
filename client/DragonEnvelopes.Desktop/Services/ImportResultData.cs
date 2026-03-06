namespace DragonEnvelopes.Desktop.Services;

public sealed record ImportPreviewResultData(
    int Parsed,
    int Valid,
    int Deduped,
    IReadOnlyList<ImportPreviewRowData> Rows);

public sealed record ImportPreviewRowData(
    int RowNumber,
    string OccurredOn,
    string Amount,
    string Merchant,
    string Description,
    string Category,
    bool IsDuplicate,
    string Errors);

public sealed record ImportCommitResultData(
    int Parsed,
    int Valid,
    int Deduped,
    int Inserted,
    int Failed);
