using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Domain.Entities;

public sealed class RecurringBill
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public string Name { get; private set; }
    public string Merchant { get; private set; }
    public Money Amount { get; private set; }
    public RecurringBillFrequency Frequency { get; private set; }
    public int DayOfMonth { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsActive { get; private set; }

    public RecurringBill(
        Guid id,
        Guid familyId,
        string name,
        string merchant,
        Money amount,
        RecurringBillFrequency frequency,
        int dayOfMonth,
        DateOnly startDate,
        DateOnly? endDate,
        bool isActive)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Recurring bill id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (dayOfMonth < 1 || dayOfMonth > 31)
        {
            throw new DomainValidationException("DayOfMonth must be between 1 and 31.");
        }

        if (endDate.HasValue && endDate.Value < startDate)
        {
            throw new DomainValidationException("EndDate cannot be earlier than StartDate.");
        }

        Id = id;
        FamilyId = familyId;
        Name = ValidateRequired(name, "Recurring bill name");
        Merchant = ValidateRequired(merchant, "Merchant");
        amount = amount.EnsureNonNegative("Amount");
        if (amount.IsZero)
        {
            throw new DomainValidationException("Amount must be greater than zero.");
        }

        Amount = amount;
        Frequency = frequency;
        DayOfMonth = dayOfMonth;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = isActive;
    }

    public void Update(
        string name,
        string merchant,
        Money amount,
        RecurringBillFrequency frequency,
        int dayOfMonth,
        DateOnly startDate,
        DateOnly? endDate,
        bool isActive)
    {
        if (dayOfMonth < 1 || dayOfMonth > 31)
        {
            throw new DomainValidationException("DayOfMonth must be between 1 and 31.");
        }

        if (endDate.HasValue && endDate.Value < startDate)
        {
            throw new DomainValidationException("EndDate cannot be earlier than StartDate.");
        }

        Name = ValidateRequired(name, "Recurring bill name");
        Merchant = ValidateRequired(merchant, "Merchant");
        amount = amount.EnsureNonNegative("Amount");
        if (amount.IsZero)
        {
            throw new DomainValidationException("Amount must be greater than zero.");
        }

        Amount = amount;
        Frequency = frequency;
        DayOfMonth = dayOfMonth;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = isActive;
    }

    private static string ValidateRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }
}
