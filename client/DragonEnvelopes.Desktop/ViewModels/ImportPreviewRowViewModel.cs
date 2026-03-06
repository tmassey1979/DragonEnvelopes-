namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record ImportPreviewRowViewModel(
    int RowNumber,
    string OccurredOn,
    string Amount,
    string Merchant,
    string Description,
    string Category,
    bool IsDuplicate,
    string Errors);
