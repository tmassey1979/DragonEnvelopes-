namespace DragonEnvelopes.Contracts.Imports;

public sealed record ImportPreviewResponse(
    int Parsed,
    int Valid,
    int Deduped,
    IReadOnlyList<ImportPreviewRowResponse> Rows);

public sealed record ImportPreviewRowResponse(
    int RowNumber,
    DateOnly? OccurredOn,
    decimal? Amount,
    string? Merchant,
    string? Description,
    string? Category,
    bool IsDuplicate,
    IReadOnlyList<string> Errors);
