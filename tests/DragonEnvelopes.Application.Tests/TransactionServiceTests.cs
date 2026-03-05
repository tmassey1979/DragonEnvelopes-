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
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var accountId = Guid.NewGuid();
        repository.Setup(x => x.GetAccountFamilyIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        repository.Setup(x => x.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<IReadOnlyList<TransactionSplitEntry>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new TransactionService(repository.Object, envelopeRepository.Object, categorizationEngine.Object, incomeAllocationEngine.Object);
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
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var accountId = Guid.NewGuid();
        repository.Setup(x => x.GetAccountFamilyIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var service = new TransactionService(repository.Object, envelopeRepository.Object, categorizationEngine.Object, incomeAllocationEngine.Object);
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
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var accountId = Guid.NewGuid();
        var splitEnvelopeId = Guid.NewGuid();
        var splitEnvelope = new Envelope(
            splitEnvelopeId,
            Guid.NewGuid(),
            "Split",
            Money.FromDecimal(200m),
            Money.FromDecimal(100m));

        repository.Setup(x => x.GetAccountFamilyIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        repository.Setup(x => x.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<IReadOnlyList<TransactionSplitEntry>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(splitEnvelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(splitEnvelope);

        var service = new TransactionService(repository.Object, envelopeRepository.Object, categorizationEngine.Object, incomeAllocationEngine.Object);
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
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
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

        var service = new TransactionService(repository.Object, envelopeRepository.Object, categorizationEngine.Object, incomeAllocationEngine.Object);
        var transactions = await service.ListAsync(accountId);

        Assert.Single(transactions);
        Assert.Equal("Groceries", transactions[0].Description);
    }

    [Fact]
    public async Task CreateAsync_WithNegativeAmountAndEnvelope_SpendsEnvelope()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var accountId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelope = new Envelope(
            envelopeId,
            Guid.NewGuid(),
            "Food",
            Money.FromDecimal(300m),
            Money.FromDecimal(200m));

        transactionRepository.Setup(x => x.GetAccountFamilyIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        transactionRepository.Setup(x => x.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<IReadOnlyList<TransactionSplitEntry>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);

        var service = new TransactionService(transactionRepository.Object, envelopeRepository.Object, categorizationEngine.Object, incomeAllocationEngine.Object);
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
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var accountId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelope = new Envelope(
            envelopeId,
            Guid.NewGuid(),
            "Food",
            Money.FromDecimal(300m),
            Money.FromDecimal(200m));

        transactionRepository.Setup(x => x.GetAccountFamilyIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        transactionRepository.Setup(x => x.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<IReadOnlyList<TransactionSplitEntry>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);

        var service = new TransactionService(transactionRepository.Object, envelopeRepository.Object, categorizationEngine.Object, incomeAllocationEngine.Object);
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

    [Fact]
    public async Task CreateAsync_WhenCategoryMissing_AppliesCategorizationRule()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var accountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();

        transactionRepository.Setup(x => x.GetAccountFamilyIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(familyId);
        categorizationEngine.Setup(x => x.EvaluateAsync(
                familyId,
                "AMZN Mktp US*123",
                "Amazon",
                -45.22m,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Shopping");
        transactionRepository.Setup(x => x.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<IReadOnlyList<TransactionSplitEntry>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new TransactionService(transactionRepository.Object, envelopeRepository.Object, categorizationEngine.Object, incomeAllocationEngine.Object);
        var result = await service.CreateAsync(
            accountId,
            -45.22m,
            "AMZN Mktp US*123",
            "Amazon",
            DateTimeOffset.UtcNow,
            null,
            null,
            false,
            null);

        Assert.Equal("Shopping", result.Category);
    }

    [Fact]
    public async Task CreateAsync_WhenCategoryProvided_SkipsCategorizationRule()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var accountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();

        transactionRepository.Setup(x => x.GetAccountFamilyIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(familyId);
        transactionRepository.Setup(x => x.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<IReadOnlyList<TransactionSplitEntry>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new TransactionService(transactionRepository.Object, envelopeRepository.Object, categorizationEngine.Object, incomeAllocationEngine.Object);
        var result = await service.CreateAsync(
            accountId,
            -12.30m,
            "Lunch",
            "Cafe",
            DateTimeOffset.UtcNow,
            "Dining",
            null,
            false,
            null);

        categorizationEngine.Verify(x => x.EvaluateAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal("Dining", result.Category);
    }

    [Fact]
    public async Task CreateAsync_WithPositiveAmount_UsesAutomaticAllocationSplits()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var accountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelope = new Envelope(
            envelopeId,
            familyId,
            "Savings",
            Money.FromDecimal(500m),
            Money.FromDecimal(100m));
        IReadOnlyList<TransactionSplitEntry>? savedSplits = null;

        transactionRepository.Setup(x => x.GetAccountFamilyIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(familyId);
        incomeAllocationEngine.Setup(x => x.AllocateAsync(
                familyId,
                "Paycheck",
                "Employer",
                200m,
                "Income",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TransactionSplitCreateDetails(envelopeId, 120m, "Income", "Auto allocation")]);
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);
        transactionRepository.Setup(x => x.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<IReadOnlyList<TransactionSplitEntry>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Transaction, IReadOnlyList<TransactionSplitEntry>, CancellationToken>((_, splits, _) => savedSplits = splits)
            .Returns(Task.CompletedTask);

        var service = new TransactionService(transactionRepository.Object, envelopeRepository.Object, categorizationEngine.Object, incomeAllocationEngine.Object);
        var result = await service.CreateAsync(
            accountId,
            200m,
            "Paycheck",
            "Employer",
            DateTimeOffset.UtcNow,
            "Income",
            null,
            false,
            null);

        Assert.Single(result.Splits);
        Assert.NotNull(savedSplits);
        Assert.Single(savedSplits!);
        Assert.Equal(120m, savedSplits![0].Amount.Amount);
        Assert.Equal(220m, envelope.CurrentBalance.Amount);
    }

    [Fact]
    public async Task CreateAsync_WithManualSplits_SkipsAutomaticAllocationEngine()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var accountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelope = new Envelope(
            envelopeId,
            familyId,
            "Bills",
            Money.FromDecimal(300m),
            Money.FromDecimal(40m));

        transactionRepository.Setup(x => x.GetAccountFamilyIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(familyId);
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);
        transactionRepository.Setup(x => x.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<IReadOnlyList<TransactionSplitEntry>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new TransactionService(transactionRepository.Object, envelopeRepository.Object, categorizationEngine.Object, incomeAllocationEngine.Object);
        var result = await service.CreateAsync(
            accountId,
            50m,
            "Transfer",
            "Internal",
            DateTimeOffset.UtcNow,
            "Income",
            null,
            true,
            [new TransactionSplitCreateDetails(envelopeId, 50m, "Income", "Manual")]);

        incomeAllocationEngine.Verify(x => x.AllocateAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
        Assert.Single(result.Splits);
    }
}
