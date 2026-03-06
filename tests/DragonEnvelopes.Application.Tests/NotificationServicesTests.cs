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
}
