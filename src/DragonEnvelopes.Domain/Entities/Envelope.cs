using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Domain.Entities;

public sealed class Envelope
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public string Name { get; private set; }
    public Money MonthlyBudget { get; private set; }
    public Money CurrentBalance { get; private set; }
    public DateTimeOffset? LastActivityAt { get; private set; }
    public bool IsArchived { get; private set; }

    public Envelope(
        Guid id,
        Guid familyId,
        string name,
        Money monthlyBudget,
        Money currentBalance)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Envelope id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        Id = id;
        FamilyId = familyId;
        Name = ValidateText(name, "Envelope name");
        MonthlyBudget = monthlyBudget.EnsureNonNegative("Monthly budget");
        CurrentBalance = currentBalance.EnsureNonNegative("Current balance");
    }

    public void Rename(string name)
    {
        EnsureNotArchived();
        Name = ValidateText(name, "Envelope name");
    }

    public void SetMonthlyBudget(Money monthlyBudget)
    {
        EnsureNotArchived();
        MonthlyBudget = monthlyBudget.EnsureNonNegative("Monthly budget");
    }

    public void Allocate(Money amount, DateTimeOffset occurredAtUtc)
    {
        EnsureNotArchived();
        if (amount <= Money.Zero)
        {
            throw new DomainValidationException("Allocation amount must be greater than zero.");
        }

        CurrentBalance += amount;
        LastActivityAt = occurredAtUtc;
    }

    public void Spend(Money amount, DateTimeOffset occurredAtUtc)
    {
        EnsureNotArchived();
        if (amount <= Money.Zero)
        {
            throw new DomainValidationException("Spend amount must be greater than zero.");
        }

        if (CurrentBalance < amount)
        {
            throw new DomainValidationException("Envelope does not have enough balance for this expense.");
        }

        CurrentBalance -= amount;
        LastActivityAt = occurredAtUtc;
    }

    public void Archive()
    {
        IsArchived = true;
    }

    private void EnsureNotArchived()
    {
        if (IsArchived)
        {
            throw new DomainValidationException("Archived envelope cannot be modified.");
        }
    }

    private static string ValidateText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }
}

