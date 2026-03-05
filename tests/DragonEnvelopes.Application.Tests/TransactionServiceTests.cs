using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public class TransactionServiceTests
{
    [Fact]
    public async Task CreateAsync_ReturnsCreatedTransaction()
    {
        var repository = new Mock<ITransactionRepository>();
        var accountId = Guid.NewGuid();
        repository.Setup(x => x.AccountExistsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(x => x.AddTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new TransactionService(repository.Object);
        var transaction = await service.CreateAsync(
            accountId,
            -15.25m,
            "Coffee",
            "Coffee Shop",
            DateTimeOffset.UtcNow,
            "Food",
            null,
            false);

        Assert.Equal(accountId, transaction.AccountId);
        Assert.Equal(-15.25m, transaction.Amount);
        Assert.Equal("Coffee", transaction.Description);
        Assert.Empty(transaction.Splits);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenAccountMissing()
    {
        var repository = new Mock<ITransactionRepository>();
        var accountId = Guid.NewGuid();
        repository.Setup(x => x.AccountExistsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new TransactionService(repository.Object);
        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.CreateAsync(
                accountId,
                10m,
                "Paycheck",
                "Payroll",
                DateTimeOffset.UtcNow,
                "Income",
                null,
                false));
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenSplitsProvided()
    {
        var repository = new Mock<ITransactionRepository>();
        var accountId = Guid.NewGuid();
        repository.Setup(x => x.AccountExistsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new TransactionService(repository.Object);
        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.CreateAsync(
                accountId,
                20m,
                "Split test",
                "Store",
                DateTimeOffset.UtcNow,
                "Misc",
                null,
                true));
    }

    [Fact]
    public async Task ListAsync_MapsTransactions()
    {
        var repository = new Mock<ITransactionRepository>();
        var accountId = Guid.NewGuid();
        repository.Setup(x => x.ListTransactionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Transaction(
                    Guid.NewGuid(),
                    accountId,
                    Money.FromDecimal(-30m),
                    "Groceries",
                    "Market",
                    DateTimeOffset.UtcNow,
                    "Food",
                    null)
            ]);

        var service = new TransactionService(repository.Object);
        var transactions = await service.ListAsync(accountId);

        Assert.Single(transactions);
        Assert.Equal("Groceries", transactions[0].Description);
    }
}
