using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Domain.Entities;

public enum EnvelopeGoalStatus
{
    Active = 1,
    Completed = 2,
    Cancelled = 3
}

public sealed class EnvelopeGoal
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public Guid EnvelopeId { get; }
    public Money TargetAmount { get; private set; }
    public DateOnly DueDate { get; private set; }
    public EnvelopeGoalStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public EnvelopeGoal(
        Guid id,
        Guid familyId,
        Guid envelopeId,
        Money targetAmount,
        DateOnly dueDate,
        EnvelopeGoalStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Envelope goal id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (envelopeId == Guid.Empty)
        {
            throw new DomainValidationException("Envelope id is required.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainValidationException("Goal status is invalid.");
        }

        Id = id;
        FamilyId = familyId;
        EnvelopeId = envelopeId;
        targetAmount = targetAmount.EnsureNonNegative("Target amount");
        if (targetAmount.IsZero)
        {
            throw new DomainValidationException("Target amount must be greater than zero.");
        }

        TargetAmount = targetAmount;
        DueDate = dueDate;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Update(Money targetAmount, DateOnly dueDate, EnvelopeGoalStatus status, DateTimeOffset updatedAtUtc)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainValidationException("Goal status is invalid.");
        }

        targetAmount = targetAmount.EnsureNonNegative("Target amount");
        if (targetAmount.IsZero)
        {
            throw new DomainValidationException("Target amount must be greater than zero.");
        }

        TargetAmount = targetAmount;
        DueDate = dueDate;
        Status = status;
        UpdatedAtUtc = updatedAtUtc;
    }
}
