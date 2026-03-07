using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Application.Tests.Fixtures;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public class EnvelopeGoalServiceTests
{
    [Fact]
    public async Task CreateUpdateDelete_Lifecycle_Works()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var goalRepository = new Mock<IEnvelopeGoalRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        EnvelopeGoal? stored = null;

        goalRepository.Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        goalRepository.Setup(x => x.EnvelopeExistsAsync(envelopeId, familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        goalRepository.Setup(x => x.ExistsForEnvelopeAsync(envelopeId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        goalRepository.Setup(x => x.AddAsync(It.IsAny<EnvelopeGoal>(), It.IsAny<CancellationToken>()))
            .Callback<EnvelopeGoal, CancellationToken>((goal, _) => stored = goal)
            .Returns(Task.CompletedTask);
        goalRepository.Setup(x => x.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored);
        goalRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        goalRepository.Setup(x => x.DeleteAsync(It.IsAny<EnvelopeGoal>(), It.IsAny<CancellationToken>()))
            .Callback<EnvelopeGoal, CancellationToken>((_, _) => stored = null)
            .Returns(Task.CompletedTask);

        var envelope = new Envelope(
            envelopeId,
            familyId,
            "Emergency",
            Money.FromDecimal(300m),
            Money.FromDecimal(140m));
        envelopeRepository.Setup(x => x.GetByIdAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);

        var service = new EnvelopeGoalService(goalRepository.Object, envelopeRepository.Object, clock.Object);
        var created = await service.CreateAsync(
            familyId,
            envelopeId,
            500m,
            new DateOnly(2026, 6, 1),
            "Active");

        var updated = await service.UpdateAsync(
            created.Id,
            650m,
            new DateOnly(2026, 9, 1),
            "Completed");

        await service.DeleteAsync(created.Id);

        Assert.Equal(500m, created.TargetAmount);
        Assert.Equal("Active", created.Status);
        Assert.Equal(650m, updated.TargetAmount);
        Assert.Equal("Completed", updated.Status);
        Assert.Null(stored);
    }

    [Fact]
    public async Task ProjectAsync_CalculatesOnTrackAndBehind()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var goalRepository = new Mock<IEnvelopeGoalRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var familyId = Guid.NewGuid();
        var envelopeOnTrackId = Guid.NewGuid();
        var envelopeBehindId = Guid.NewGuid();

        var onTrackGoal = new EnvelopeGoal(
            Guid.NewGuid(),
            familyId,
            envelopeOnTrackId,
            Money.FromDecimal(300m),
            new DateOnly(2026, 3, 1),
            EnvelopeGoalStatus.Active,
            fixture.FrozenUtcNow,
            fixture.FrozenUtcNow);
        var behindGoal = new EnvelopeGoal(
            Guid.NewGuid(),
            familyId,
            envelopeBehindId,
            Money.FromDecimal(500m),
            new DateOnly(2026, 3, 1),
            EnvelopeGoalStatus.Active,
            fixture.FrozenUtcNow,
            fixture.FrozenUtcNow);

        goalRepository.Setup(x => x.ListByFamilyAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([onTrackGoal, behindGoal]);

        envelopeRepository.Setup(x => x.ListByFamilyAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Envelope(
                    envelopeOnTrackId,
                    familyId,
                    "Emergency",
                    Money.FromDecimal(300m),
                    Money.FromDecimal(250m)),
                new Envelope(
                    envelopeBehindId,
                    familyId,
                    "Travel",
                    Money.FromDecimal(200m),
                    Money.FromDecimal(50m))
            ]);

        var service = new EnvelopeGoalService(goalRepository.Object, envelopeRepository.Object, clock.Object);
        var projection = await service.ProjectAsync(
            familyId,
            new DateOnly(2026, 2, 15));

        Assert.Equal(2, projection.Count);
        var emergency = projection.Single(item => item.EnvelopeName == "Emergency");
        var travel = projection.Single(item => item.EnvelopeName == "Travel");
        Assert.Equal("OnTrack", emergency.ProjectionStatus);
        Assert.Equal("Behind", travel.ProjectionStatus);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenEnvelopeAlreadyHasGoal()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var goalRepository = new Mock<IEnvelopeGoalRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();

        goalRepository.Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        goalRepository.Setup(x => x.EnvelopeExistsAsync(envelopeId, familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        goalRepository.Setup(x => x.ExistsForEnvelopeAsync(envelopeId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EnvelopeGoalService(goalRepository.Object, envelopeRepository.Object, clock.Object);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.CreateAsync(
            familyId,
            envelopeId,
            100m,
            new DateOnly(2026, 6, 1),
            "Active"));
    }
}
