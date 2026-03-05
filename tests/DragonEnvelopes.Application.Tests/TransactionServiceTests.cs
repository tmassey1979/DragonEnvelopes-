using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Application.DTOs;
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
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var accountId = Guid.NewGuid();
        repository.Setup(x => x.AccountExistsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(x => x.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<IReadOnlyList<TransactionSplitEntry>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new TransactionService(repository.Object, envelopeRepository.Object);
        var transaction = await service.CreateAsync(
            accountId,
            -15.25m,
            "Coffee",
            "Coffee Shop",
            DateTimeOffset.UtcNow,
            "Food",
            null,
            false,
            null);

        Assert.Equal(accountId, transaction.AccountId);
        Assert.Equal(-15.25m, transaction.Amount);
        Assert.Equal("Coffee", transaction.Description);
        Assert.Empty(transaction.Splits);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenAccountMissing()
    {
        var repository = new Mock<ITransactionRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var accountId = Guid.NewGuid();
        repository.Setup(x => x.AccountExistsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new TransactionService(repository.Object, envelopeRepository.Object);
        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.CreateAsync(
                accountId,
                10m,
                "Paycheck",
                "Payroll",
                DateTimeOffset.UtcNow,
                "Income",
                null,
                false,
                null));
    }

    [Fact]
    public async Task CreateAsync_WithSplits_PersistsSplitEntries()
    {
        var repository = new Mock<ITransactionRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var accountId = Guid.NewGuid();
        var splitEnvelopeId = Guid.NewGuid();
        var splitEnvelope = new Envelope(
            splitEnvelopeId,
            Guid.NewGuid(),
            "Split",
            Money.FromDecimal(200m),
            Money.FromDecimal(100m));

        repository.Setup(x => x.AccountExistsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(x => x.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<IReadOnlyList<TransactionSplitEntry>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(splitEnvelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(splitEnvelope);

        var service = new TransactionService(repository.Object, envelopeRepository.Object);
        var result = await service.CreateAsync(
            accountId,
            -20m,
            "Split test",
            "Store",
            DateTimeOffset.UtcNow,
            "Misc",
            null,
            true,
            [new TransactionSplitCreateDetails(splitEnvelopeId, -20m, "Misc", "note")]);

        Assert.Single(result.Splits);
        Assert.Equal("note", result.Splits[0].Notes);
    }

    [Fact]
    public async Task ListAsync_MapsTransactions()
    {
        var repository = new Mock<ITransactionRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
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
        repository.Setup(x => x.ListTransactionSplitsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new TransactionService(repository.Object, envelopeRepository.Object);
        var transactions = await service.ListAsync(accountId);

        Assert.Single(transactions);
        Assert.Equal("Groceries", transactions[0].Description);
    }

    [Fact]
    public async Task CreateAsync_WithNegativeAmountAndEnvelope_SpendsEnvelope()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var accountId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelope = new Envelope(
            envelopeId,
            Guid.NewGuid(),
            "Food",
            Money.FromDecimal(300m),
            Money.FromDecimal(200m));

        transactionRepository.Setup(x => x.AccountExistsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        transactionRepository.Setup(x => x.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<IReadOnlyList<TransactionSplitEntry>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);

        var service = new TransactionService(transactionRepository.Object, envelopeRepository.Object);
        await service.CreateAsync(
            accountId,
            -25m,
            "Lunch",
            "Cafe",
            DateTimeOffset.UtcNow,
            "Food",
            envelopeId,
            false,
            null);

        Assert.Equal(175m, envelope.CurrentBalance.Amount);
    }

    [Fact]
    public async Task CreateAsync_WithPositiveAmountAndEnvelope_AllocatesEnvelope()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var accountId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelope = new Envelope(
            envelopeId,
            Guid.NewGuid(),
            "Food",
            Money.FromDecimal(300m),
            Money.FromDecimal(200m));

        transactionRepository.Setup(x => x.AccountExistsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        transactionRepository.Setup(x => x.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<IReadOnlyList<TransactionSplitEntry>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);

        var service = new TransactionService(transactionRepository.Object, envelopeRepository.Object);
        await service.CreateAsync(
            accountId,
            40m,
            "Budget top-up",
            "Transfer",
            DateTimeOffset.UtcNow,
            "Food",
            envelopeId,
            false,
            null);

        Assert.Equal(240m, envelope.CurrentBalance.Amount);
    }
}
