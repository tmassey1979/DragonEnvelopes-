namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record CardControlAuditItemViewModel(
    Guid Id,
    string Action,
    string ChangedBy,
    string ChangedAt,
    string PreviousStateJson,
    string NewStateJson);
