using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public sealed class NotificationServicesTests
{
    [Fact]
    public async Task QueueSpendNotificationsAsync_RespectsParentPreferences()
    {
        var now = new DateTimeOffset(2026, 3, 6, 22, 0, 0, TimeSpan.Zero);
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var parentUserId = "parent-user";

        var familyRepository = new Mock<IFamilyRepository>();
        familyRepository
            .Setup(x => x.ListMembersAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new FamilyMember(
                    Guid.NewGuid(),
                    familyId,
                    parentUserId,
                    "Parent User",
                    EmailAddress.Parse("parent@test.dev"),
                    MemberRole.Parent),
                new FamilyMember(
                    Guid.NewGuid(),
                    familyId,
                    "teen-user",
                    "Teen User",
                    EmailAddress.Parse("teen@test.dev"),
                    MemberRole.Teen)
            ]);

        var preferenceRepository = new Mock<INotificationPreferenceRepository>();
        preferenceRepository
            .Setup(x => x.GetByFamilyAndUserAsync(familyId, parentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationPreference(
                Guid.NewGuid(),
                familyId,
                parentUserId,
                emailEnabled: false,
                inAppEnabled: true,
                smsEnabled: true,
                now));

        IReadOnlyCollection<SpendNotificationEvent>? queued = null;
        var eventRepository = new Mock<ISpendNotificationEventRepository>();
        eventRepository
            .Setup(x => x.AddRangeAsync(It.IsAny<IReadOnlyCollection<SpendNotificationEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<SpendNotificationEvent>, CancellationToken>((events, _) => queued = events)
            .Returns(Task.CompletedTask);
        eventRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now);

        var service = new ParentSpendNotificationService(
            familyRepository.Object,
            preferenceRepository.Object,
            eventRepository.Object,
            clock.Object);

        var result = await service.QueueSpendNotificationsAsync(
            familyId,
            envelopeId,
            cardId,
            "evt_1",
            amount: 12.34m,
            merchant: "Target",
            remainingBalance: 87.66m);

        Assert.Equal(2, result.GeneratedCount);
        Assert.NotNull(queued);
        Assert.Contains(queued!, x => x.Channel == "InApp");
        Assert.Contains(queued!, x => x.Channel == "Sms");
        Assert.DoesNotContain(queued!, x => x.Channel == "Email");
    }

    [Fact]
    public async Task DispatchPendingAsync_SendsNonSms_AndRetriesSms()
    {
        var now = new DateTimeOffset(2026, 3, 6, 22, 0, 0, TimeSpan.Zero);
        var queued = new List<SpendNotificationEvent>
        {
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "parent-1",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "evt_1",
                "Email",
                10m,
                "Target",
                90m,
                now),
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "parent-2",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "evt_1",
                "Sms",
                15m,
                "Walmart",
                85m,
                now)
        };

        var eventRepository = new Mock<ISpendNotificationEventRepository>();
        eventRepository
            .Setup(x => x.ListDispatchableAsync(3, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queued);
        eventRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(1));

        var service = new SpendNotificationDispatchService(
            eventRepository.Object,
            clock.Object,
            Mock.Of<ILogger<SpendNotificationDispatchService>>());

        var result = await service.DispatchPendingAsync();

        Assert.Equal(1, result.SentCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal("Sent", queued[0].Status);
        Assert.Equal("Queued", queued[1].Status);
        Assert.Equal(1, queued[1].AttemptCount);
    }

    [Fact]
    public async Task ListFailedEventsAsync_ReturnsMappedFailedEvents()
    {
        var familyId = Guid.NewGuid();
        var failed = new SpendNotificationEvent(
            Guid.NewGuid(),
            familyId,
            "parent-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "evt_failed",
            "Email",
            22m,
            "Electric Co",
            78m,
            DateTimeOffset.UtcNow);
        failed.MarkRetry("attempt 1", DateTimeOffset.UtcNow, 3);
        failed.MarkRetry("attempt 2", DateTimeOffset.UtcNow, 3);
        failed.MarkRetry("attempt 3", DateTimeOffset.UtcNow, 3);

        var eventRepository = new Mock<ISpendNotificationEventRepository>();
        eventRepository
            .Setup(x => x.ListFailedByFamilyAsync(familyId, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync([failed]);

        var service = new SpendNotificationDispatchService(
            eventRepository.Object,
            Mock.Of<IClock>(),
            Mock.Of<ILogger<SpendNotificationDispatchService>>());

        var result = await service.ListFailedEventsAsync(familyId);

        var mapped = Assert.Single(result);
        Assert.Equal(failed.Id, mapped.Id);
        Assert.Equal("Failed", mapped.Status);
        Assert.Equal(3, mapped.AttemptCount);
    }

    [Fact]
    public async Task RetryFailedEventAsync_MarksEventAsSentWhenDeliverySucceeds()
    {
        var familyId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var failed = new SpendNotificationEvent(
            eventId,
            familyId,
            "parent-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "evt_retry",
            "Email",
            14m,
            "Grocery",
            86m,
            DateTimeOffset.UtcNow.AddMinutes(-10));
        failed.MarkRetry("attempt 1", DateTimeOffset.UtcNow.AddMinutes(-9), 3);
        failed.MarkRetry("attempt 2", DateTimeOffset.UtcNow.AddMinutes(-8), 3);
        failed.MarkRetry("attempt 3", DateTimeOffset.UtcNow.AddMinutes(-7), 3);

        var eventRepository = new Mock<ISpendNotificationEventRepository>();
        eventRepository
            .Setup(x => x.GetByFamilyAndIdForUpdateAsync(familyId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failed);
        eventRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var now = DateTimeOffset.UtcNow;
        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now);

        var service = new SpendNotificationDispatchService(
            eventRepository.Object,
            clock.Object,
            Mock.Of<ILogger<SpendNotificationDispatchService>>());

        var result = await service.RetryFailedEventAsync(familyId, eventId);

        Assert.Equal("Sent", result.Status);
        Assert.Equal(4, result.AttemptCount);
        Assert.Equal(now, result.SentAtUtc);
        Assert.Equal("Sent", failed.Status);
    }

    [Fact]
    public async Task ReplayEventAsync_IsIdempotent_ForAlreadySentEvent()
    {
        var familyId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var sentAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var sent = new SpendNotificationEvent(
            eventId,
            familyId,
            "parent-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "evt_sent",
            "Email",
            11m,
            "Fuel",
            89m,
            DateTimeOffset.UtcNow.AddMinutes(-10));
        sent.MarkSent(sentAt);

        var eventRepository = new Mock<ISpendNotificationEventRepository>();
        eventRepository
            .Setup(x => x.GetByFamilyAndIdForUpdateAsync(familyId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sent);

        var service = new SpendNotificationDispatchService(
            eventRepository.Object,
            Mock.Of<IClock>(),
            Mock.Of<ILogger<SpendNotificationDispatchService>>());

        var result = await service.ReplayEventAsync(familyId, eventId);

        Assert.Equal("Sent", result.Status);
        Assert.Equal(1, result.AttemptCount);
        Assert.Equal(sentAt, result.SentAtUtc);
    }

    [Fact]
    public async Task DispatchPendingAsync_EnqueuesFailedOutboxEvent_WhenDispatchBecomesTerminalFailure()
    {
        var now = new DateTimeOffset(2026, 3, 6, 22, 0, 0, TimeSpan.Zero);
        var failedOnThirdAttempt = new SpendNotificationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "parent-2",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "evt_terminal",
            "Sms",
            15m,
            "Walmart",
            85m,
            now);
        failedOnThirdAttempt.MarkRetry("attempt 1", now.AddMinutes(1), 3);
        failedOnThirdAttempt.MarkRetry("attempt 2", now.AddMinutes(2), 3);

        var eventRepository = new Mock<ISpendNotificationEventRepository>();
        eventRepository
            .Setup(x => x.ListDispatchableAsync(3, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([failedOnThirdAttempt]);
        eventRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(3));
        var outboxRepository = new Mock<IIntegrationOutboxRepository>();

        var service = new SpendNotificationDispatchService(
            eventRepository.Object,
            clock.Object,
            Mock.Of<ILogger<SpendNotificationDispatchService>>(),
            outboxRepository.Object);

        await service.DispatchPendingAsync();

        outboxRepository.Verify(
            x => x.AddAsync(
                It.Is<IntegrationOutboxMessage>(message =>
                    message.SourceService == "financial-api"
                    && message.RoutingKey == "financial.provider-notification.dispatch-failed.v1"
                    && message.EventName == "ProviderNotificationDispatchFailed"
                    && message.FamilyId == failedOnThirdAttempt.FamilyId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RetryFailedEventAsync_EnqueuesRetriedOutboxEvent_WhenDeliverySucceeds()
    {
        var familyId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var failed = new SpendNotificationEvent(
            eventId,
            familyId,
            "parent-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "evt_retry",
            "Email",
            14m,
            "Grocery",
            86m,
            DateTimeOffset.UtcNow.AddMinutes(-10));
        failed.MarkRetry("attempt 1", DateTimeOffset.UtcNow.AddMinutes(-9), 3);
        failed.MarkRetry("attempt 2", DateTimeOffset.UtcNow.AddMinutes(-8), 3);
        failed.MarkRetry("attempt 3", DateTimeOffset.UtcNow.AddMinutes(-7), 3);

        var eventRepository = new Mock<ISpendNotificationEventRepository>();
        eventRepository
            .Setup(x => x.GetByFamilyAndIdForUpdateAsync(familyId, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failed);
        eventRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var now = DateTimeOffset.UtcNow;
        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now);
        var outboxRepository = new Mock<IIntegrationOutboxRepository>();

        var service = new SpendNotificationDispatchService(
            eventRepository.Object,
            clock.Object,
            Mock.Of<ILogger<SpendNotificationDispatchService>>(),
            outboxRepository.Object);

        await service.RetryFailedEventAsync(familyId, eventId);

        outboxRepository.Verify(
            x => x.AddAsync(
                It.Is<IntegrationOutboxMessage>(message =>
                    message.SourceService == "financial-api"
                    && message.RoutingKey == "financial.provider-notification.dispatch-retried.v1"
                    && message.EventName == "ProviderNotificationDispatchRetried"
                    && message.FamilyId == familyId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
