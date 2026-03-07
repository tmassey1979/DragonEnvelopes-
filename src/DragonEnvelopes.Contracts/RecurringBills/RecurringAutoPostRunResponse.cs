namespace DragonEnvelopes.Contracts.RecurringBills;

public sealed record RecurringAutoPostRunResponse(
    Guid FamilyId,
    DateOnly DueDate,
    int DueBillCount,
    int PostedCount,
    int SkippedCount,
    int FailedCount,
    int AlreadyProcessedCount,
    IReadOnlyList<RecurringAutoPostExecutionResponse> Executions);
