namespace DragonEnvelopes.Ledger.Api.Services;

public sealed record RecurringAutoPostExecutionItem(
    Guid RecurringBillId,
    string RecurringBillName,
    string Result,
    Guid? TransactionId,
    string? Notes);

public sealed record RecurringAutoPostRunSummary(
    Guid? FamilyId,
    DateOnly DueDate,
    int DueBillCount,
    int PostedCount,
    int SkippedCount,
    int FailedCount,
    int AlreadyProcessedCount,
    IReadOnlyList<RecurringAutoPostExecutionItem> Executions);
