namespace DragonEnvelopes.Contracts.RecurringBills;

public sealed record RecurringAutoPostExecutionResponse(
    Guid RecurringBillId,
    string RecurringBillName,
    string Result,
    Guid? TransactionId,
    string? Notes);
