using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using Microsoft.Extensions.Options;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public sealed class SpendAnomalyServiceTests
{
    [Fact]
    public async Task DetectAndRecordAsync_MerchantSpike_Persists_Anomaly_Event()
    {
        var familyId = Guid.Parse("b1000000-0000-0000-0000-000000000001");
        var transactionId = Guid.Parse("b1000000-0000-0000-0000-000000000002");
        var accountId = Guid.Parse("b1000000-0000-0000-0000-000000000003");
        var detectedAt = new DateTimeOffset(2026, 3, 7, 16, 30, 0, TimeSpan.Zero);

        var repository = new Mock<ISpendAnomalyEventRepository>(MockBehavior.Strict);
        repository
            .Setup(x => x.ExistsForTransactionAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(x => x.ListRecentSpendSamplesAsync(
                familyId,
                It.IsAny<DateTimeOffset>(),
                transactionId,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SpendAnomalySample(Guid.NewGuid(), "Dragon Mart", -21m, detectedAt.AddDays(-10)),
                new SpendAnomalySample(Guid.NewGuid(), "Dragon Mart", -22m, detectedAt.AddDays(-9)),
                new SpendAnomalySample(Guid.NewGuid(), "Dragon Mart", -19m, detectedAt.AddDays(-8)),
                new SpendAnomalySample(Guid.NewGuid(), "Dragon Mart", -20m, detectedAt.AddDays(-7)),
                new SpendAnomalySample(Guid.NewGuid(), "Fuel Stop", -45m, detectedAt.AddDays(-6))
            ]);

        SpendAnomalyEvent? persistedEvent = null;
        repository
            .Setup(x => x.AddAsync(It.IsAny<SpendAnomalyEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SpendAnomalyEvent, CancellationToken>((eventItem, _) => persistedEvent = eventItem)
            .Returns(Task.CompletedTask);
        repository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clock = new Mock<IClock>(MockBehavior.Strict);
        clock.SetupGet(x => x.UtcNow).Returns(detectedAt);

        var service = new SpendAnomalyService(
            repository.Object,
            clock.Object,
            Options.Create(new SpendAnomalyDetectionOptions
            {
                MinimumAbsoluteAmount = 10m
            }));

        await service.DetectAndRecordAsync(
            familyId,
            transactionId,
            accountId,
            "Dragon Mart",
            -96m,
            detectedAt,
            CancellationToken.None);

        Assert.NotNull(persistedEvent);
        Assert.Equal(familyId, persistedEvent!.FamilyId);
        Assert.Equal(transactionId, persistedEvent.TransactionId);
        Assert.Equal(accountId, persistedEvent.AccountId);
        Assert.Equal(96m, persistedEvent.Amount);
        Assert.True(persistedEvent.SeverityScore >= 55);
        Assert.Contains("Dragon Mart", persistedEvent.Reason);

        repository.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task DetectAndRecordAsync_NormalSpend_DoesNotPersist_Anomaly_Event()
    {
        var familyId = Guid.Parse("b2000000-0000-0000-0000-000000000001");
        var transactionId = Guid.Parse("b2000000-0000-0000-0000-000000000002");
        var accountId = Guid.Parse("b2000000-0000-0000-0000-000000000003");
        var occurredAt = new DateTimeOffset(2026, 3, 7, 17, 0, 0, TimeSpan.Zero);

        var repository = new Mock<ISpendAnomalyEventRepository>(MockBehavior.Strict);
        repository
            .Setup(x => x.ExistsForTransactionAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(x => x.ListRecentSpendSamplesAsync(
                familyId,
                It.IsAny<DateTimeOffset>(),
                transactionId,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SpendAnomalySample(Guid.NewGuid(), "Corner Shop", -22m, occurredAt.AddDays(-12)),
                new SpendAnomalySample(Guid.NewGuid(), "Corner Shop", -19m, occurredAt.AddDays(-10)),
                new SpendAnomalySample(Guid.NewGuid(), "Corner Shop", -21m, occurredAt.AddDays(-8)),
                new SpendAnomalySample(Guid.NewGuid(), "Corner Shop", -20m, occurredAt.AddDays(-6)),
                new SpendAnomalySample(Guid.NewGuid(), "Corner Shop", -23m, occurredAt.AddDays(-4))
            ]);

        var service = new SpendAnomalyService(
            repository.Object,
            Mock.Of<IClock>(),
            Options.Create(new SpendAnomalyDetectionOptions
            {
                MinimumAbsoluteAmount = 10m
            }));

        await service.DetectAndRecordAsync(
            familyId,
            transactionId,
            accountId,
            "Corner Shop",
            -22m,
            occurredAt,
            CancellationToken.None);

        repository.Verify(
            x => x.AddAsync(It.IsAny<SpendAnomalyEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        repository.VerifyAll();
    }
}
