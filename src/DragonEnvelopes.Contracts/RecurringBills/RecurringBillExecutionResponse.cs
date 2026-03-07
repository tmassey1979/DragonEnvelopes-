namespace DragonEnvelopes.Contracts.RecurringBills;

public sealed record RecurringBillExecutionResponse(
    Guid Id,
    Guid RecurringBillId,
    Guid FamilyId,
    DateOnly DueDate,
    DateTimeOffset ExecutedAtUtc,
    Guid? TransactionId,
    string Result,
    string? Notes,
    string IdempotencyKey);
