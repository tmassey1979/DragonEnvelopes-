using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;
using Microsoft.Extensions.Options;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public sealed class DataRetentionServiceTests
{
    [Fact]
    public async Task CleanupAsync_DeletesUsingConfiguredCutoffsAndBatchSize()
    {
        var now = new DateTimeOffset(2026, 3, 6, 12, 0, 0, TimeSpan.Zero);
        var options = new DataRetentionOptions
        {
            BatchSize = 250,
            StripeWebhookRetentionDays = 45,
            SpendNotificationRetentionDays = 30
        };

        DateTimeOffset? stripeCutoff = null;
        DateTimeOffset? notificationCutoff = null;
        var stripeRepository = new Mock<IStripeWebhookEventRepository>();
        stripeRepository
            .Setup(x => x.DeleteProcessedBeforeAsync(It.IsAny<DateTimeOffset>(), options.BatchSize, It.IsAny<CancellationToken>()))
            .Callback<DateTimeOffset, int, CancellationToken>((cutoff, _, _) => stripeCutoff = cutoff)
            .ReturnsAsync(11);

        var notificationRepository = new Mock<ISpendNotificationEventRepository>();
        notificationRepository
            .Setup(x => x.DeleteTerminalBeforeAsync(It.IsAny<DateTimeOffset>(), options.BatchSize, It.IsAny<CancellationToken>()))
            .Callback<DateTimeOffset, int, CancellationToken>((cutoff, _, _) => notificationCutoff = cutoff)
            .ReturnsAsync(7);

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now);

        var service = new DataRetentionService(
            stripeRepository.Object,
            notificationRepository.Object,
            clock.Object,
            Options.Create(options));

        var result = await service.CleanupAsync();

        Assert.Equal(now, result.ExecutedAtUtc);
        Assert.Equal(now.AddDays(-45), result.StripeWebhookCutoffUtc);
        Assert.Equal(now.AddDays(-30), result.SpendNotificationCutoffUtc);
        Assert.Equal(11, result.DeletedStripeWebhookEvents);
        Assert.Equal(7, result.DeletedSpendNotificationEvents);
        Assert.Equal(now.AddDays(-45), stripeCutoff);
        Assert.Equal(now.AddDays(-30), notificationCutoff);
    }

    [Fact]
    public async Task CleanupAsync_NormalizesInvalidConfigurationValues()
    {
        var now = new DateTimeOffset(2026, 3, 6, 12, 0, 0, TimeSpan.Zero);
        var options = new DataRetentionOptions
        {
            BatchSize = 0,
            StripeWebhookRetentionDays = 0,
            SpendNotificationRetentionDays = -10
        };

        var stripeRepository = new Mock<IStripeWebhookEventRepository>();
        stripeRepository
            .Setup(x => x.DeleteProcessedBeforeAsync(now.AddDays(-1), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var notificationRepository = new Mock<ISpendNotificationEventRepository>();
        notificationRepository
            .Setup(x => x.DeleteTerminalBeforeAsync(now.AddDays(-1), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now);

        var service = new DataRetentionService(
            stripeRepository.Object,
            notificationRepository.Object,
            clock.Object,
            Options.Create(options));

        await service.CleanupAsync();

        stripeRepository.VerifyAll();
        notificationRepository.VerifyAll();
    }
}
