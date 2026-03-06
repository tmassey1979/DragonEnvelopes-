using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public sealed class EnvelopePaymentCardControlServiceTests
{
    [Fact]
    public async Task UpsertControlsAsync_CreatesControl_AndAudit()
    {
        var now = new DateTimeOffset(2026, 3, 6, 22, 0, 0, TimeSpan.Zero);
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var envelopeRepository = new Mock<IEnvelopeRepository>();
        envelopeRepository
            .Setup(x => x.GetByIdAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Envelope(envelopeId, familyId, "Kids", Money.FromDecimal(200m), Money.FromDecimal(50m)));

        var cardRepository = new Mock<IEnvelopePaymentCardRepository>();
        cardRepository
            .Setup(x => x.GetByIdForUpdateAsync(cardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnvelopePaymentCard(
                cardId,
                familyId,
                envelopeId,
                Guid.NewGuid(),
                "Stripe",
                "card_123",
                "Virtual",
                "Active",
                "Visa",
                "4242",
                now,
                now));

        var controlRepository = new Mock<IEnvelopePaymentCardControlRepository>();
        controlRepository
            .Setup(x => x.GetByCardIdForUpdateAsync(cardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnvelopePaymentCardControl?)null);
        controlRepository
            .Setup(x => x.AddAsync(It.IsAny<EnvelopePaymentCardControl>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        controlRepository
            .Setup(x => x.AddAuditAsync(It.IsAny<EnvelopePaymentCardControlAudit>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        controlRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var stripeGateway = new Mock<IStripeGateway>();
        stripeGateway
            .Setup(x => x.UpdateCardSpendingControlsAsync(
                "card_123",
                25m,
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(1));

        var service = new EnvelopePaymentCardControlService(
            envelopeRepository.Object,
            cardRepository.Object,
            controlRepository.Object,
            stripeGateway.Object,
            clock.Object);

        var result = await service.UpsertControlsAsync(
            familyId,
            envelopeId,
            cardId,
            25m,
            ["grocery_stores", "restaurants"],
            ["Aldi", "Target"],
            "user-a");

        Assert.Equal(25m, result.DailyLimitAmount);
        Assert.Contains("grocery_stores", result.AllowedMerchantCategories);
        Assert.Contains("Target", result.AllowedMerchantNames);
        controlRepository.Verify(x => x.AddAsync(It.IsAny<EnvelopePaymentCardControl>(), It.IsAny<CancellationToken>()), Times.Once);
        controlRepository.Verify(x => x.AddAuditAsync(It.IsAny<EnvelopePaymentCardControlAudit>(), It.IsAny<CancellationToken>()), Times.Once);
        controlRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpsertControlsAsync_WhenProviderSyncFails_DoesNotPersist()
    {
        var now = new DateTimeOffset(2026, 3, 6, 22, 0, 0, TimeSpan.Zero);
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var envelopeRepository = new Mock<IEnvelopeRepository>();
        envelopeRepository
            .Setup(x => x.GetByIdAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Envelope(envelopeId, familyId, "Kids", Money.FromDecimal(200m), Money.FromDecimal(50m)));

        var cardRepository = new Mock<IEnvelopePaymentCardRepository>();
        cardRepository
            .Setup(x => x.GetByIdForUpdateAsync(cardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnvelopePaymentCard(
                cardId,
                familyId,
                envelopeId,
                Guid.NewGuid(),
                "Stripe",
                "card_123",
                "Virtual",
                "Active",
                "Visa",
                "4242",
                now,
                now));

        var controlRepository = new Mock<IEnvelopePaymentCardControlRepository>(MockBehavior.Strict);
        var stripeGateway = new Mock<IStripeGateway>();
        stripeGateway
            .Setup(x => x.UpdateCardSpendingControlsAsync(
                "card_123",
                25m,
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainValidationException("Provider failed"));

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(1));

        var service = new EnvelopePaymentCardControlService(
            envelopeRepository.Object,
            cardRepository.Object,
            controlRepository.Object,
            stripeGateway.Object,
            clock.Object);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.UpsertControlsAsync(
            familyId,
            envelopeId,
            cardId,
            25m,
            ["grocery_stores"],
            null,
            "user-a"));

        controlRepository.Verify(x => x.AddAsync(It.IsAny<EnvelopePaymentCardControl>(), It.IsAny<CancellationToken>()), Times.Never);
        controlRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateSpendAsync_Denies_WhenMerchantIsNotAllowed()
    {
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var created = new DateTimeOffset(2026, 3, 6, 22, 0, 0, TimeSpan.Zero);

        var envelopeRepository = new Mock<IEnvelopeRepository>(MockBehavior.Strict);
        var cardRepository = new Mock<IEnvelopePaymentCardRepository>();
        cardRepository
            .Setup(x => x.GetByIdAsync(cardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnvelopePaymentCard(
                cardId,
                familyId,
                envelopeId,
                Guid.NewGuid(),
                "Stripe",
                "card_123",
                "Virtual",
                "Active",
                "Visa",
                "4242",
                created,
                created));

        var controlRepository = new Mock<IEnvelopePaymentCardControlRepository>();
        controlRepository
            .Setup(x => x.GetByCardIdAsync(cardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnvelopePaymentCardControl(
                Guid.NewGuid(),
                familyId,
                envelopeId,
                cardId,
                100m,
                "[\"grocery_stores\"]",
                "[\"Aldi\"]",
                created,
                created));

        var stripeGateway = new Mock<IStripeGateway>(MockBehavior.Strict);
        var clock = new Mock<IClock>(MockBehavior.Strict);

        var service = new EnvelopePaymentCardControlService(
            envelopeRepository.Object,
            cardRepository.Object,
            controlRepository.Object,
            stripeGateway.Object,
            clock.Object);

        var result = await service.EvaluateSpendAsync(
            familyId,
            envelopeId,
            cardId,
            "Gas Station",
            "grocery_stores",
            20m,
            10m);

        Assert.False(result.IsAllowed);
        Assert.Equal("MerchantNotAllowed", result.DenialReason);
    }

    [Fact]
    public async Task EvaluateSpendAsync_Denies_WhenDailyLimitWouldBeExceeded()
    {
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var created = new DateTimeOffset(2026, 3, 6, 22, 0, 0, TimeSpan.Zero);

        var envelopeRepository = new Mock<IEnvelopeRepository>(MockBehavior.Strict);
        var cardRepository = new Mock<IEnvelopePaymentCardRepository>();
        cardRepository
            .Setup(x => x.GetByIdAsync(cardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnvelopePaymentCard(
                cardId,
                familyId,
                envelopeId,
                Guid.NewGuid(),
                "Stripe",
                "card_123",
                "Virtual",
                "Active",
                "Visa",
                "4242",
                created,
                created));

        var controlRepository = new Mock<IEnvelopePaymentCardControlRepository>();
        controlRepository
            .Setup(x => x.GetByCardIdAsync(cardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnvelopePaymentCardControl(
                Guid.NewGuid(),
                familyId,
                envelopeId,
                cardId,
                50m,
                "[\"grocery_stores\"]",
                "[\"Aldi\"]",
                created,
                created));

        var stripeGateway = new Mock<IStripeGateway>(MockBehavior.Strict);
        var clock = new Mock<IClock>(MockBehavior.Strict);

        var service = new EnvelopePaymentCardControlService(
            envelopeRepository.Object,
            cardRepository.Object,
            controlRepository.Object,
            stripeGateway.Object,
            clock.Object);

        var result = await service.EvaluateSpendAsync(
            familyId,
            envelopeId,
            cardId,
            "Aldi",
            "grocery_stores",
            20m,
            40m);

        Assert.False(result.IsAllowed);
        Assert.Equal("DailyLimitExceeded", result.DenialReason);
        Assert.Equal(10m, result.RemainingDailyLimit);
    }
}
