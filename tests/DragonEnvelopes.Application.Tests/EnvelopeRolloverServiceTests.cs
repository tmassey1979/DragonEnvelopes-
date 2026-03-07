using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public class EnvelopeRolloverServiceTests
{
    [Fact]
    public async Task ApplyAsync_WhenRunAlreadyExists_ReturnsAlreadyApplied()
    {
        var familyId = Guid.NewGuid();
        var run = new EnvelopeRolloverRun(
            Guid.NewGuid(),
            familyId,
            "2026-03",
            DateTimeOffset.Parse("2026-03-31T12:00:00Z"),
            appliedByUserId: "existing-user",
            envelopeCount: 1,
            totalRolloverBalance: 50m,
            """{"items":[{"envelopeId":"00000000-0000-0000-0000-000000000001","envelopeName":"Groceries","currentBalance":60.00,"rolloverMode":"Cap","rolloverCap":50.00,"rolloverBalance":50.00,"adjustmentAmount":-10.00}]}""");

        var envelopeRepository = new Mock<IEnvelopeRepository>();
        envelopeRepository.Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var runRepository = new Mock<IEnvelopeRolloverRunRepository>();
        runRepository.Setup(x => x.GetByFamilyAndMonthAsync(familyId, "2026-03", It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var clock = new Mock<IClock>();
        var service = new EnvelopeRolloverService(envelopeRepository.Object, runRepository.Object, clock.Object);

        var result = await service.ApplyAsync(familyId, "2026-03", "new-user");

        Assert.True(result.AlreadyApplied);
        Assert.Equal(run.Id, result.RunId);
        Assert.Single(result.Items);
        runRepository.Verify(
            x => x.AddAsync(It.IsAny<EnvelopeRolloverRun>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApplyAsync_AdjustsActiveEnvelopeBalancesAndPersistsRun()
    {
        var familyId = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-03-31T23:59:59Z");
        var groceriesId = Guid.NewGuid();
        var funId = Guid.NewGuid();

        var groceries = new Envelope(
            groceriesId,
            familyId,
            "Groceries",
            Money.FromDecimal(400m),
            Money.FromDecimal(120m),
            EnvelopeRolloverMode.Cap,
            Money.FromDecimal(100m));
        var fun = new Envelope(
            funId,
            familyId,
            "Fun",
            Money.FromDecimal(200m),
            Money.FromDecimal(80m),
            EnvelopeRolloverMode.None,
            rolloverCap: null);

        var envelopeRepository = new Mock<IEnvelopeRepository>();
        envelopeRepository.Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        envelopeRepository.Setup(x => x.ListByFamilyForUpdateAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([groceries, fun]);

        var runRepository = new Mock<IEnvelopeRolloverRunRepository>();
        runRepository.Setup(x => x.GetByFamilyAndMonthAsync(familyId, "2026-03", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnvelopeRolloverRun?)null);
        runRepository.Setup(x => x.AddAsync(It.IsAny<EnvelopeRolloverRun>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now);

        var service = new EnvelopeRolloverService(envelopeRepository.Object, runRepository.Object, clock.Object);
        var result = await service.ApplyAsync(familyId, "2026-03", "parent-user");

        Assert.False(result.AlreadyApplied);
        Assert.Equal(2, result.EnvelopeCount);
        Assert.Equal(100m, groceries.CurrentBalance.Amount);
        Assert.Equal(0m, fun.CurrentBalance.Amount);
        Assert.Equal(-20m, result.Items.Single(x => x.EnvelopeId == groceriesId).AdjustmentAmount);
        Assert.Equal(-80m, result.Items.Single(x => x.EnvelopeId == funId).AdjustmentAmount);
        runRepository.Verify(
            x => x.AddAsync(It.Is<EnvelopeRolloverRun>(run => run.FamilyId == familyId && run.Month == "2026-03"), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
