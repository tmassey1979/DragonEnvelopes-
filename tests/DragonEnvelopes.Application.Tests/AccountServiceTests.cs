using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public class AccountServiceTests
{
    [Fact]
    public async Task CreateAsync_ReturnsCreatedAccount()
    {
        var repository = new Mock<IAccountRepository>();
        var familyId = Guid.NewGuid();
        repository.Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(x => x.AccountNameExistsAsync(familyId, "Checking", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(x => x.AddAccountAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new AccountService(repository.Object);
        var account = await service.CreateAsync(familyId, "Checking", "Checking", 1250.50m);

        Assert.Equal(familyId, account.FamilyId);
        Assert.Equal("Checking", account.Name);
        Assert.Equal("Checking", account.Type);
        Assert.Equal(1250.50m, account.Balance);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenFamilyMissing()
    {
        var repository = new Mock<IAccountRepository>();
        var familyId = Guid.NewGuid();
        repository.Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new AccountService(repository.Object);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.CreateAsync(familyId, "Checking", "Checking", 0m));
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenTypeInvalid()
    {
        var repository = new Mock<IAccountRepository>();
        var familyId = Guid.NewGuid();
        repository.Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(x => x.AccountNameExistsAsync(familyId, "Checking", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new AccountService(repository.Object);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.CreateAsync(familyId, "Checking", "NotARealType", 0m));
    }

    [Fact]
    public async Task ListAsync_MapsAccounts()
    {
        var repository = new Mock<IAccountRepository>();
        var familyId = Guid.NewGuid();
        repository.Setup(x => x.ListAccountsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Account(Guid.NewGuid(), familyId, "Savings", AccountType.Savings, Money.FromDecimal(20m)),
                new Account(Guid.NewGuid(), familyId, "Wallet", AccountType.Cash, Money.FromDecimal(10m))
            ]);

        var service = new AccountService(repository.Object);
        var accounts = await service.ListAsync(familyId);

        Assert.Equal(2, accounts.Count);
        Assert.Contains(accounts, x => x.Name == "Savings" && x.Type == "Savings");
        Assert.Contains(accounts, x => x.Name == "Wallet" && x.Type == "Cash");
    }
}
