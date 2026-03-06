namespace DragonEnvelopes.Domain.Entities;

public sealed class RecurringBillExecution
{
    public Guid Id { get; }
    public Guid RecurringBillId { get; }
    public Guid FamilyId { get; }
    public DateOnly DueDate { get; }
    public DateTimeOffset ExecutedAtUtc { get; }
    public Guid? TransactionId { get; }
    public string Result { get; }
    public string? Notes { get; }

    public RecurringBillExecution(
        Guid id,
        Guid recurringBillId,
        Guid familyId,
        DateOnly dueDate,
        DateTimeOffset executedAtUtc,
        Guid? transactionId,
        string result,
        string? notes = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Recurring bill execution id is required.");
        }

        if (recurringBillId == Guid.Empty)
        {
            throw new DomainValidationException("Recurring bill id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (string.IsNullOrWhiteSpace(result))
        {
            throw new DomainValidationException("Execution result is required.");
        }

        Id = id;
        RecurringBillId = recurringBillId;
        FamilyId = familyId;
        DueDate = dueDate;
        ExecutedAtUtc = executedAtUtc;
        TransactionId = transactionId == Guid.Empty ? null : transactionId;
        Result = result.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}
