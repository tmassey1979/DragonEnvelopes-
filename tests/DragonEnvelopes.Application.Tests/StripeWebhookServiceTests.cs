using System.Security.Cryptography;
using System.Text;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public sealed class StripeWebhookServiceTests
{
    [Fact]
    public async Task ProcessAsync_WithInvalidSignature_ReturnsInvalidSignature()
    {
        var cardRepository = new Mock<IEnvelopePaymentCardRepository>(MockBehavior.Strict);
        var envelopeRepository = new Mock<IEnvelopeRepository>(MockBehavior.Strict);
        var eventRepository = new Mock<IStripeWebhookEventRepository>(MockBehavior.Strict);
        var parentSpendNotificationService = new Mock<IParentSpendNotificationService>(MockBehavior.Strict);
        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000));

        var service = new StripeWebhookService(
            cardRepository.Object,
            envelopeRepository.Object,
            eventRepository.Object,
            parentSpendNotificationService.Object,
            clock.Object,
            Options.Create(new StripeWebhookOptions
            {
                Enabled = true,
                SigningSecret = "whsec_test",
                SignatureToleranceSeconds = 300
            }),
            Mock.Of<ILogger<StripeWebhookService>>());

        var payload = "{\"id\":\"evt_1\",\"type\":\"card_transaction\",\"data\":{\"object\":{\"card\":\"card_1\",\"amount\":1000}}}";
        var result = await service.ProcessAsync(payload, "t=1700000000,v1=invalidsig");

        Assert.Equal("InvalidSignature", result.Outcome);
    }

    [Fact]
    public async Task ProcessAsync_WhenDuplicateEvent_ReturnsDuplicate()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var cardRepository = new Mock<IEnvelopePaymentCardRepository>(MockBehavior.Strict);
        var envelopeRepository = new Mock<IEnvelopeRepository>(MockBehavior.Strict);
        var eventRepository = new Mock<IStripeWebhookEventRepository>();
        var parentSpendNotificationService = new Mock<IParentSpendNotificationService>(MockBehavior.Strict);
        eventRepository
            .Setup(x => x.GetByEventIdAsync("evt_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StripeWebhookEvent(
                Guid.NewGuid(),
                "evt_1",
                "card_transaction",
                null,
                null,
                null,
                "Processed",
                null,
                "{}",
                now,
                now));

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now);

        var service = new StripeWebhookService(
            cardRepository.Object,
            envelopeRepository.Object,
            eventRepository.Object,
            parentSpendNotificationService.Object,
            clock.Object,
            Options.Create(new StripeWebhookOptions
            {
                Enabled = true,
                SigningSecret = "whsec_test",
                SignatureToleranceSeconds = 300
            }),
            Mock.Of<ILogger<StripeWebhookService>>());

        var payload = "{\"id\":\"evt_1\",\"type\":\"card_transaction\",\"data\":{\"object\":{\"card\":\"card_1\",\"amount\":1000}}}";
        var signature = BuildSignatureHeader(payload, "whsec_test", 1_700_000_000);
        var result = await service.ProcessAsync(payload, signature);

        Assert.Equal("Duplicate", result.Outcome);
        eventRepository.Verify(x => x.AddAsync(It.IsAny<StripeWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_CardTransaction_SpendsEnvelope_AndRecordsEvent()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var cardRepository = new Mock<IEnvelopePaymentCardRepository>();
        cardRepository
            .Setup(x => x.GetByProviderCardIdForUpdateAsync("Stripe", "card_provider_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnvelopePaymentCard(
                cardId,
                familyId,
                envelopeId,
                Guid.NewGuid(),
                "Stripe",
                "card_provider_1",
                "Virtual",
                "Active",
                "Visa",
                "4242",
                now,
                now));

        var envelope = new Envelope(
            envelopeId,
            familyId,
            "Groceries",
            Money.FromDecimal(200m),
            Money.FromDecimal(100m));
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        envelopeRepository
            .Setup(x => x.GetByIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);

        StripeWebhookEvent? persistedEvent = null;
        var eventRepository = new Mock<IStripeWebhookEventRepository>();
        eventRepository
            .Setup(x => x.GetByEventIdAsync("evt_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((StripeWebhookEvent?)null);
        eventRepository
            .Setup(x => x.AddAsync(It.IsAny<StripeWebhookEvent>(), It.IsAny<CancellationToken>()))
            .Callback<StripeWebhookEvent, CancellationToken>((evt, _) => persistedEvent = evt)
            .Returns(Task.CompletedTask);
        eventRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var parentSpendNotificationService = new Mock<IParentSpendNotificationService>();
        parentSpendNotificationService
            .Setup(x => x.QueueSpendNotificationsAsync(
                familyId,
                envelopeId,
                cardId,
                "evt_1",
                5m,
                It.IsAny<string>(),
                95m,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpendNotificationQueueResult(2));

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now);

        var service = new StripeWebhookService(
            cardRepository.Object,
            envelopeRepository.Object,
            eventRepository.Object,
            parentSpendNotificationService.Object,
            clock.Object,
            Options.Create(new StripeWebhookOptions
            {
                Enabled = true,
                SigningSecret = "whsec_test",
                SignatureToleranceSeconds = 300
            }),
            Mock.Of<ILogger<StripeWebhookService>>());

        var payload = "{\"id\":\"evt_1\",\"type\":\"card_transaction\",\"data\":{\"object\":{\"card\":\"card_provider_1\",\"amount\":500}}}";
        var signature = BuildSignatureHeader(payload, "whsec_test", 1_700_000_000);
        var result = await service.ProcessAsync(payload, signature);

        Assert.Equal("Processed", result.Outcome);
        Assert.Equal(95m, envelope.CurrentBalance.Amount);
        Assert.NotNull(persistedEvent);
        Assert.Equal("Processed", persistedEvent!.ProcessingStatus);
        parentSpendNotificationService.VerifyAll();
    }

    [Fact]
    public async Task ProcessAsync_WhenSpendFails_RecordsFailedEvent()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var cardRepository = new Mock<IEnvelopePaymentCardRepository>();
        cardRepository
            .Setup(x => x.GetByProviderCardIdForUpdateAsync("Stripe", "card_provider_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnvelopePaymentCard(
                cardId,
                familyId,
                envelopeId,
                Guid.NewGuid(),
                "Stripe",
                "card_provider_1",
                "Virtual",
                "Active",
                "Visa",
                "4242",
                now,
                now));

        var envelope = new Envelope(
            envelopeId,
            familyId,
            "Groceries",
            Money.FromDecimal(200m),
            Money.FromDecimal(1m));
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        envelopeRepository
            .Setup(x => x.GetByIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);

        StripeWebhookEvent? persistedEvent = null;
        var eventRepository = new Mock<IStripeWebhookEventRepository>();
        eventRepository
            .Setup(x => x.GetByEventIdAsync("evt_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((StripeWebhookEvent?)null);
        eventRepository
            .Setup(x => x.AddAsync(It.IsAny<StripeWebhookEvent>(), It.IsAny<CancellationToken>()))
            .Callback<StripeWebhookEvent, CancellationToken>((evt, _) => persistedEvent = evt)
            .Returns(Task.CompletedTask);
        eventRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var parentSpendNotificationService = new Mock<IParentSpendNotificationService>(MockBehavior.Strict);

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now);

        var service = new StripeWebhookService(
            cardRepository.Object,
            envelopeRepository.Object,
            eventRepository.Object,
            parentSpendNotificationService.Object,
            clock.Object,
            Options.Create(new StripeWebhookOptions
            {
                Enabled = true,
                SigningSecret = "whsec_test",
                SignatureToleranceSeconds = 300
            }),
            Mock.Of<ILogger<StripeWebhookService>>());

        var payload = "{\"id\":\"evt_1\",\"type\":\"card_transaction\",\"data\":{\"object\":{\"card\":\"card_provider_1\",\"amount\":500}}}";
        var signature = BuildSignatureHeader(payload, "whsec_test", 1_700_000_000);
        var result = await service.ProcessAsync(payload, signature);

        Assert.Equal("Failed", result.Outcome);
        Assert.NotNull(persistedEvent);
        Assert.Equal("Failed", persistedEvent!.ProcessingStatus);
    }

    private static string BuildSignatureHeader(string payload, string secret, long timestamp)
    {
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
        return $"t={timestamp},v1={hash}";
    }
}
