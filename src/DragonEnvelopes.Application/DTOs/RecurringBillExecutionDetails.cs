namespace DragonEnvelopes.Application.DTOs;

public sealed record RecurringBillExecutionDetails(
    Guid Id,
    Guid RecurringBillId,
    Guid FamilyId,
    DateOnly DueDate,
    DateTimeOffset ExecutedAtUtc,
    Guid? TransactionId,
    string Result,
    string? Notes,
    string IdempotencyKey);
