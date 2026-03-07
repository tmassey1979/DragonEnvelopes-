using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public class EnvelopeTransferServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenSourceAndDestinationMatch_Throws()
    {
        var envelopeId = Guid.NewGuid();
        var service = new EnvelopeTransferService(
            new Mock<IEnvelopeRepository>().Object,
            new Mock<ITransactionRepository>().Object);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.CreateAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            envelopeId,
            envelopeId,
            10m,
            DateTimeOffset.UtcNow,
            notes: null));
    }

    [Fact]
    public async Task CreateAsync_WhenInsufficientFunds_Throws()
    {
        var familyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var fromEnvelopeId = Guid.NewGuid();
        var toEnvelopeId = Guid.NewGuid();
        var fromEnvelope = new Envelope(fromEnvelopeId, familyId, "From", Money.FromDecimal(100m), Money.FromDecimal(5m));
        var toEnvelope = new Envelope(toEnvelopeId, familyId, "To", Money.FromDecimal(100m), Money.FromDecimal(0m));

        var envelopeRepository = new Mock<IEnvelopeRepository>();
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(fromEnvelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromEnvelope);
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(toEnvelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(toEnvelope);

        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository.Setup(x => x.AccountBelongsToFamilyAsync(accountId, familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EnvelopeTransferService(envelopeRepository.Object, transactionRepository.Object);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.CreateAsync(
            familyId,
            accountId,
            fromEnvelopeId,
            toEnvelopeId,
            10m,
            DateTimeOffset.UtcNow,
            notes: null));
    }

    [Fact]
    public async Task CreateAsync_CreatesTwoLinkedTransferTransactionsAndAdjustsEnvelopeBalances()
    {
        var familyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var fromEnvelopeId = Guid.NewGuid();
        var toEnvelopeId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.Parse("2026-03-07T10:30:00Z");
        var fromEnvelope = new Envelope(fromEnvelopeId, familyId, "From", Money.FromDecimal(100m), Money.FromDecimal(80m));
        var toEnvelope = new Envelope(toEnvelopeId, familyId, "To", Money.FromDecimal(100m), Money.FromDecimal(20m));
        IReadOnlyList<Transaction>? capturedTransactions = null;

        var envelopeRepository = new Mock<IEnvelopeRepository>();
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(fromEnvelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromEnvelope);
        envelopeRepository.Setup(x => x.GetByIdForUpdateAsync(toEnvelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(toEnvelope);

        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository.Setup(x => x.AccountBelongsToFamilyAsync(accountId, familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        transactionRepository.Setup(x => x.AddTransactionsAsync(It.IsAny<IReadOnlyList<Transaction>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Transaction>, CancellationToken>((items, _) => capturedTransactions = items)
            .Returns(Task.CompletedTask);

        var service = new EnvelopeTransferService(envelopeRepository.Object, transactionRepository.Object);
        var result = await service.CreateAsync(
            familyId,
            accountId,
            fromEnvelopeId,
            toEnvelopeId,
            25m,
            occurredAt,
            "rebalance");

        Assert.NotNull(capturedTransactions);
        Assert.Equal(2, capturedTransactions!.Count);
        Assert.All(capturedTransactions, transaction => Assert.Equal(result.TransferId, transaction.TransferId));
        Assert.Contains(capturedTransactions, transaction => transaction.TransferDirection == "Debit" && transaction.Amount.Amount == -25m);
        Assert.Contains(capturedTransactions, transaction => transaction.TransferDirection == "Credit" && transaction.Amount.Amount == 25m);
        Assert.Equal(55m, fromEnvelope.CurrentBalance.Amount);
        Assert.Equal(45m, toEnvelope.CurrentBalance.Amount);
    }
}
