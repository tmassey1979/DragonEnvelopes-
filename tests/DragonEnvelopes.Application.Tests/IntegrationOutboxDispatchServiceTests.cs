using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Application.Tests.Fixtures;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public sealed class IntegrationOutboxDispatchServiceTests
{
    private const string SourceService = "family-api";

    [Fact]
    public async Task DispatchPendingAsync_Publishes_Message_And_Marks_Dispatched()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IIntegrationOutboxRepository>();
        var publisher = new Mock<IIntegrationOutboxMessagePublisher>();
        var logger = new Mock<ILogger<IntegrationOutboxDispatchService>>();

        var message = CreateMessage(fixture.FrozenUtcNow.AddMinutes(-2));
        repository.Setup(x => x.ListDispatchableAsync(fixture.FrozenUtcNow, 50, SourceService, It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.CountPendingAsync(SourceService, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        publisher.Setup(x => x.PublishAsync(It.IsAny<IntegrationOutboxEnvelopeMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new IntegrationOutboxDispatchService(
            repository.Object,
            publisher.Object,
            clock.Object,
            logger.Object);

        var result = await service.DispatchPendingAsync(SourceService, 50);

        Assert.Equal(1, result.LoadedCount);
        Assert.Equal(1, result.PublishedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(0, result.PendingCount);
        Assert.Equal(fixture.FrozenUtcNow, message.DispatchedAtUtc);
        Assert.Equal(0, message.AttemptCount);
        Assert.Null(message.LastError);

        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        publisher.Verify(x => x.PublishAsync(It.IsAny<IntegrationOutboxEnvelopeMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyMetricLog(logger, "event.publish.lag.seconds", Times.AtLeastOnce());
        VerifyMetricLog(logger, "event.publish.published.count", Times.AtLeastOnce());
        VerifyMetricLog(logger, "event.publish.backlog.count", Times.AtLeastOnce());
    }

    [Fact]
    public async Task DispatchPendingAsync_When_Publish_Fails_Schedules_Retry()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IIntegrationOutboxRepository>();
        var publisher = new Mock<IIntegrationOutboxMessagePublisher>();
        var logger = new Mock<ILogger<IntegrationOutboxDispatchService>>();

        var createdAt = fixture.FrozenUtcNow.AddMinutes(-1);
        var message = CreateMessage(createdAt);
        repository.Setup(x => x.ListDispatchableAsync(fixture.FrozenUtcNow, 25, SourceService, It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.CountPendingAsync(SourceService, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        publisher.Setup(x => x.PublishAsync(It.IsAny<IntegrationOutboxEnvelopeMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ unavailable"));

        var service = new IntegrationOutboxDispatchService(
            repository.Object,
            publisher.Object,
            clock.Object,
            logger.Object);

        var result = await service.DispatchPendingAsync(SourceService, 25);

        Assert.Equal(1, result.LoadedCount);
        Assert.Equal(0, result.PublishedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(1, result.PendingCount);
        Assert.Equal(1, message.AttemptCount);
        Assert.Null(message.DispatchedAtUtc);
        Assert.NotNull(message.LastError);
        Assert.True(message.NextAttemptAtUtc > fixture.FrozenUtcNow);

        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyMetricLog(logger, "event.publish.failure.count", Times.AtLeastOnce());
        VerifyMetricLog(logger, "event.publish.backlog.count", Times.AtLeastOnce());
    }

    [Fact]
    public async Task DispatchPendingAsync_When_No_Dispatchable_Messages_Returns_Current_Backlog()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IIntegrationOutboxRepository>();
        var publisher = new Mock<IIntegrationOutboxMessagePublisher>();
        var logger = new Mock<ILogger<IntegrationOutboxDispatchService>>();

        repository.Setup(x => x.ListDispatchableAsync(fixture.FrozenUtcNow, 10, SourceService, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<IntegrationOutboxMessage>());
        repository.Setup(x => x.CountPendingAsync(SourceService, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var service = new IntegrationOutboxDispatchService(
            repository.Object,
            publisher.Object,
            clock.Object,
            logger.Object);

        var result = await service.DispatchPendingAsync(SourceService, 10);

        Assert.Equal(0, result.LoadedCount);
        Assert.Equal(0, result.PublishedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(4, result.PendingCount);

        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(x => x.PublishAsync(It.IsAny<IntegrationOutboxEnvelopeMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyMetricLog(logger, "event.publish.backlog.count", Times.AtLeastOnce());
    }

    private static void VerifyMetricLog(
        Mock<ILogger<IntegrationOutboxDispatchService>> logger,
        string metricName,
        Times times)
    {
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("EventPipelineMetric", StringComparison.Ordinal)
                    && state.ToString()!.Contains(metricName, StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    private static IntegrationOutboxMessage CreateMessage(DateTimeOffset createdAtUtc)
    {
        return new IntegrationOutboxMessage(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid().ToString("D"),
            IntegrationEventRoutingKeys.FamilyCreatedV1,
            FamilyIntegrationEventNames.FamilyCreated,
            "1.0",
            "family-api",
            Guid.NewGuid().ToString("D"),
            causationId: null,
            "{\"familyName\":\"Dragon\"}",
            createdAtUtc,
            createdAtUtc);
    }
}
