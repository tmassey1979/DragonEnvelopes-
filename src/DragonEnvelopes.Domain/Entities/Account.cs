using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Domain.Entities;

public sealed class Account
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public string Name { get; private set; }
    public Money Balance { get; private set; }
    public AccountType Type { get; private set; }

    public Account(
        Guid id,
        Guid familyId,
        string name,
        AccountType type,
        Money balance)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Account id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        Id = id;
        FamilyId = familyId;
        Name = ValidateText(name, "Account name");
        Type = type;
        Balance = balance;
    }

    public void Rename(string name)
    {
        Name = ValidateText(name, "Account name");
    }

    public void ChangeType(AccountType type)
    {
        Type = type;
    }

    public void Deposit(Money amount)
    {
        if (amount <= Money.Zero)
        {
            throw new DomainValidationException("Deposit amount must be greater than zero.");
        }

        Balance += amount;
    }

    public void Withdraw(Money amount)
    {
        if (amount <= Money.Zero)
        {
            throw new DomainValidationException("Withdrawal amount must be greater than zero.");
        }

        if (Balance < amount)
        {
            throw new DomainValidationException("Insufficient account balance.");
        }

        Balance -= amount;
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
