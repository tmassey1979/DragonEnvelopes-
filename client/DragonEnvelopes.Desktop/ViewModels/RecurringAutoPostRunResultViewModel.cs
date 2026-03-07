namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record RecurringAutoPostRunResultViewModel(
    DateOnly DueDate,
    int DueBillCount,
    int PostedCount,
    int SkippedCount,
    int FailedCount,
    int AlreadyProcessedCount,
    IReadOnlyList<RecurringAutoPostExecutionItemViewModel> Executions);
