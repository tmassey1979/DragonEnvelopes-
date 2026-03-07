using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Cqrs.Messaging;
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
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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
    public async Task CreateAsync_WhenCategoryAutoAssigned_EnqueuesCategorizationExecutionOutboxMessage()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var outboxRepository = new Mock<IIntegrationOutboxRepository>();
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

        var service = new TransactionService(
            transactionRepository.Object,
            envelopeRepository.Object,
            categorizationEngine.Object,
            incomeAllocationEngine.Object,
            spendAnomalyService: null,
            integrationOutboxRepository: outboxRepository.Object);
        await service.CreateAsync(
            accountId,
            -45.22m,
            "AMZN Mktp US*123",
            "Amazon",
            DateTimeOffset.UtcNow,
            null,
            null,
            false,
            null);

        outboxRepository.Verify(
            x => x.AddAsync(
                It.Is<IntegrationOutboxMessage>(message =>
                    message.SourceService == IntegrationEventSourceServices.AutomationApi
                    && message.RoutingKey == IntegrationEventRoutingKeys.AutomationRuleExecutedV1
                    && message.EventName == AutomationIntegrationEventNames.AutomationRuleExecuted
                    && message.FamilyId == familyId
                    && message.PayloadJson.Contains("\"ExecutionType\":\"Categorization\"")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        outboxRepository.Verify(
            x => x.AddAsync(
                It.Is<IntegrationOutboxMessage>(message =>
                    message.SourceService == "ledger-api"
                    && message.RoutingKey == IntegrationEventRoutingKeys.LedgerTransactionCreatedV1
                    && message.EventName == LedgerIntegrationEventNames.TransactionCreated
                    && message.FamilyId == familyId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCategoryProvided_SkipsCategorizationRule()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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
    public async Task CreateAsync_WhenAutoAllocationUsed_EnqueuesAllocationExecutionOutboxMessage()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var outboxRepository = new Mock<IIntegrationOutboxRepository>();
        var accountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelope = new Envelope(
            envelopeId,
            familyId,
            "Savings",
            Money.FromDecimal(500m),
            Money.FromDecimal(100m));

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
            .Returns(Task.CompletedTask);

        var service = new TransactionService(
            transactionRepository.Object,
            envelopeRepository.Object,
            categorizationEngine.Object,
            incomeAllocationEngine.Object,
            spendAnomalyService: null,
            integrationOutboxRepository: outboxRepository.Object);
        await service.CreateAsync(
            accountId,
            200m,
            "Paycheck",
            "Employer",
            DateTimeOffset.UtcNow,
            "Income",
            null,
            false,
            null);

        outboxRepository.Verify(
            x => x.AddAsync(
                It.Is<IntegrationOutboxMessage>(message =>
                    message.SourceService == IntegrationEventSourceServices.AutomationApi
                    && message.RoutingKey == IntegrationEventRoutingKeys.AutomationRuleExecutedV1
                    && message.EventName == AutomationIntegrationEventNames.AutomationRuleExecuted
                    && message.FamilyId == familyId
                    && message.PayloadJson.Contains("\"ExecutionType\":\"Allocation\"")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        outboxRepository.Verify(
            x => x.AddAsync(
                It.Is<IntegrationOutboxMessage>(message =>
                    message.SourceService == "ledger-api"
                    && message.RoutingKey == IntegrationEventRoutingKeys.LedgerTransactionCreatedV1
                    && message.EventName == LedgerIntegrationEventNames.TransactionCreated
                    && message.FamilyId == familyId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithManualSplits_SkipsAutomaticAllocationEngine()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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

    [Fact]
    public async Task DeleteAsync_WithSingleEnvelope_ReversesEnvelopeAndSoftDeletesTransaction()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var envelopeId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var envelope = new Envelope(
            envelopeId,
            Guid.NewGuid(),
            "Groceries",
            Money.FromDecimal(300m),
            Money.FromDecimal(90m));
        var transaction = new Transaction(
            transactionId,
            Guid.NewGuid(),
            Money.FromDecimal(-10m),
            "Groceries",
            "Store",
            occurredAt,
            "Food",
            envelopeId);

        transactionRepository.Setup(x => x.GetTransactionByIdForUpdateAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        transactionRepository.Setup(x => x.GetAccountFamilyIdAsync(transaction.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        transactionRepository.Setup(x => x.ListTransactionSplitsByTransactionIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new TransactionService(transactionRepository.Object, envelopeRepository.Object, categorizationEngine.Object, incomeAllocationEngine.Object);
        await service.DeleteAsync(transactionId, "user-a");

        Assert.Equal(100m, envelope.CurrentBalance.Amount);
        Assert.NotNull(transaction.DeletedAtUtc);
        Assert.Equal("user-a", transaction.DeletedByUserId);
        transactionRepository.Verify(x => x.DeleteTransactionAsync(transactionId, It.IsAny<CancellationToken>()), Times.Never);
        transactionRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithSplits_ReversesEachSplitEnvelopeAndSoftDeletesTransaction()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var transactionId = Guid.NewGuid();
        var envelopeAId = Guid.NewGuid();
        var envelopeBId = Guid.NewGuid();
        var envelopeA = new Envelope(
            envelopeAId,
            Guid.NewGuid(),
            "Envelope A",
            Money.FromDecimal(200m),
            Money.FromDecimal(94m));
        var envelopeB = new Envelope(
            envelopeBId,
            Guid.NewGuid(),
            "Envelope B",
            Money.FromDecimal(200m),
            Money.FromDecimal(96m));
        var transaction = new Transaction(
            transactionId,
            Guid.NewGuid(),
            Money.FromDecimal(-10m),
            "Split Purchase",
            "Store",
            DateTimeOffset.UtcNow,
            "Food",
            envelopeId: null);
        var splits = new[]
        {
            new TransactionSplitEntry(Guid.NewGuid(), transactionId, envelopeAId, Money.FromDecimal(-6m), "Food", null),
            new TransactionSplitEntry(Guid.NewGuid(), transactionId, envelopeBId, Money.FromDecimal(-4m), "Food", null)
        };

        transactionRepository.Setup(x => x.GetTransactionByIdForUpdateAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        transactionRepository.Setup(x => x.GetAccountFamilyIdAsync(transaction.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        transactionRepository.Setup(x => x.ListTransactionSplitsByTransactionIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(splits);
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(envelopeAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelopeA);
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(envelopeBId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelopeB);
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new TransactionService(transactionRepository.Object, envelopeRepository.Object, categorizationEngine.Object, incomeAllocationEngine.Object);
        await service.DeleteAsync(transactionId, "user-a");

        Assert.Equal(100m, envelopeA.CurrentBalance.Amount);
        Assert.Equal(100m, envelopeB.CurrentBalance.Amount);
        Assert.NotNull(transaction.DeletedAtUtc);
        Assert.Equal("user-a", transaction.DeletedByUserId);
        transactionRepository.Verify(x => x.DeleteTransactionAsync(transactionId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenTransactionMissing_Throws()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var transactionId = Guid.NewGuid();

        transactionRepository.Setup(x => x.GetTransactionByIdForUpdateAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var service = new TransactionService(transactionRepository.Object, envelopeRepository.Object, categorizationEngine.Object, incomeAllocationEngine.Object);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.DeleteAsync(transactionId, "user-a"));
    }

    [Fact]
    public async Task RestoreAsync_WithSingleEnvelope_ReappliesEnvelopeAndClearsDeleteMarker()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var envelopeId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var envelope = new Envelope(
            envelopeId,
            Guid.NewGuid(),
            "Groceries",
            Money.FromDecimal(300m),
            Money.FromDecimal(100m));
        var transaction = new Transaction(
            transactionId,
            Guid.NewGuid(),
            Money.FromDecimal(-10m),
            "Groceries",
            "Store",
            occurredAt,
            "Food",
            envelopeId);
        transaction.SoftDelete(DateTimeOffset.UtcNow.AddMinutes(-5), "user-a");

        transactionRepository.Setup(x => x.GetTransactionByIdForUpdateAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        transactionRepository.Setup(x => x.GetAccountFamilyIdAsync(transaction.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        transactionRepository.Setup(x => x.ListTransactionSplitsByTransactionIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        transactionRepository.Setup(x => x.ListTransactionSplitsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);

        var service = new TransactionService(transactionRepository.Object, envelopeRepository.Object, categorizationEngine.Object, incomeAllocationEngine.Object);
        var restored = await service.RestoreAsync(transactionId);

        Assert.Equal(90m, envelope.CurrentBalance.Amount);
        Assert.Null(transaction.DeletedAtUtc);
        Assert.Null(transaction.DeletedByUserId);
        Assert.Null(restored.DeletedAtUtc);
    }

    [Fact]
    public async Task ListDeletedAsync_ClampsDaysWindow_AndMapsDeletedMetadata()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var categorizationEngine = new Mock<ICategorizationRuleEngine>();
        var incomeAllocationEngine = new Mock<IIncomeAllocationEngine>();
        var familyId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        DateTimeOffset? capturedDeletedSince = null;

        var transaction = new Transaction(
            transactionId,
            accountId,
            Money.FromDecimal(-15m),
            "Deleted transaction",
            "Store",
            now.AddDays(-3),
            "Misc",
            envelopeId: null);
        transaction.SoftDelete(now.AddDays(-2), "user-a");

        transactionRepository.Setup(x => x.ListDeletedTransactionsByFamilyAsync(familyId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, DateTimeOffset, CancellationToken>((_, deletedSinceUtc, _) => capturedDeletedSince = deletedSinceUtc)
            .ReturnsAsync([transaction]);
        transactionRepository.Setup(x => x.ListTransactionSplitsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new TransactionService(transactionRepository.Object, envelopeRepository.Object, categorizationEngine.Object, incomeAllocationEngine.Object);
        var deleted = await service.ListDeletedAsync(familyId, 500);

        Assert.NotNull(capturedDeletedSince);
        Assert.InRange(capturedDeletedSince!.Value, now.AddDays(-91), now.AddDays(-89));
        Assert.Single(deleted);
        Assert.Equal(transactionId, deleted[0].Id);
        Assert.NotNull(deleted[0].DeletedAtUtc);
        Assert.Equal("user-a", deleted[0].DeletedByUserId);
    }
}


