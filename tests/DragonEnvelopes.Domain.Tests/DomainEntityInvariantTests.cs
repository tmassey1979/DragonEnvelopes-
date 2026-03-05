using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Domain.Tests;

public class DomainEntityInvariantTests
{
    [Fact]
    public void Family_AddMember_RejectsDuplicateKeycloakUserId()
    {
        var family = new Family(Guid.NewGuid(), "Household", DateTimeOffset.UtcNow);
        var email = EmailAddress.Parse("user@example.com");
        family.AddMember(new FamilyMember(Guid.NewGuid(), family.Id, "kc-1", "User 1", email, MemberRole.Parent));

        var duplicate = new FamilyMember(Guid.NewGuid(), family.Id, "kc-1", "User 2", EmailAddress.Parse("user2@example.com"), MemberRole.Adult);

        Assert.Throws<DomainValidationException>(() => family.AddMember(duplicate));
    }

    [Fact]
    public void Account_Withdraw_RejectsInsufficientBalance()
    {
        var account = new Account(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Checking",
            AccountType.Checking,
            Money.FromDecimal(100m));

        Assert.Throws<DomainValidationException>(() => account.Withdraw(Money.FromDecimal(125m)));
    }

    [Fact]
    public void Envelope_Spend_RejectsAmountsOverCurrentBalance()
    {
        var envelope = new Envelope(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Groceries",
            Money.FromDecimal(200m),
            Money.FromDecimal(60m));

        Assert.Throws<DomainValidationException>(() => envelope.Spend(Money.FromDecimal(75m), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Transaction_SetSplits_RequiresSplitTotalsMatchAmount()
    {
        var transaction = new Transaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Money.FromDecimal(100m),
            "Target purchase",
            "Target",
            DateTimeOffset.UtcNow);

        var splits = new[]
        {
            new TransactionSplit(Guid.NewGuid(), Money.FromDecimal(20m), "Household"),
            new TransactionSplit(Guid.NewGuid(), Money.FromDecimal(30m), "Groceries")
        };

        Assert.Throws<DomainValidationException>(() => transaction.SetSplits(splits));
    }

    [Fact]
    public void Budget_SetAllocation_RejectsOverAllocation()
    {
        var budget = new Budget(
            Guid.NewGuid(),
            Guid.NewGuid(),
            BudgetMonth.Parse("2026-03"),
            Money.FromDecimal(1000m));

        budget.SetAllocation(Guid.NewGuid(), Money.FromDecimal(800m));

        Assert.Throws<DomainValidationException>(
            () => budget.SetAllocation(Guid.NewGuid(), Money.FromDecimal(300m)));
    }
}

