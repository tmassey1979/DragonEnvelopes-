namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record AuthDiagnosticEventItemViewModel(
    string OccurredAt,
    string Level,
    string Message);
