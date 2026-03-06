using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public sealed class EnvelopePaymentCardServiceTests
{
    [Fact]
    public async Task IssueVirtualCardAsync_CreatesCard()
    {
        var now = new DateTimeOffset(2026, 3, 6, 20, 0, 0, TimeSpan.Zero);
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelopeFinancialAccountId = Guid.NewGuid();

        var envelopeRepository = new Mock<IEnvelopeRepository>();
        envelopeRepository
            .Setup(x => x.GetByIdAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Envelope(envelopeId, familyId, "Groceries", Money.FromDecimal(100m), Money.FromDecimal(25m)));

        var envelopeFinancialAccountRepository = new Mock<IEnvelopeFinancialAccountRepository>();
        envelopeFinancialAccountRepository
            .Setup(x => x.GetByEnvelopeIdAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnvelopeFinancialAccount(
                envelopeFinancialAccountId,
                familyId,
                envelopeId,
                "Stripe",
                "fa_123",
                now,
                now));

        var envelopePaymentCardRepository = new Mock<IEnvelopePaymentCardRepository>();
        envelopePaymentCardRepository
            .Setup(x => x.AddAsync(It.IsAny<EnvelopePaymentCard>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var envelopePaymentCardShipmentRepository = new Mock<IEnvelopePaymentCardShipmentRepository>();

        var stripeGateway = new Mock<IStripeGateway>();
        stripeGateway
            .Setup(x => x.CreateVirtualCardAsync("fa_123", familyId, envelopeId, "Kid Card", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("card_123", "Active", "Visa", "4242"));

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(1));

        var service = new EnvelopePaymentCardService(
            envelopeRepository.Object,
            envelopeFinancialAccountRepository.Object,
            envelopePaymentCardRepository.Object,
            envelopePaymentCardShipmentRepository.Object,
            stripeGateway.Object,
            clock.Object);

        var result = await service.IssueVirtualCardAsync(familyId, envelopeId, "Kid Card");

        Assert.Equal("card_123", result.ProviderCardId);
        Assert.Equal("Active", result.Status);
        Assert.Equal("Virtual", result.Type);
        envelopePaymentCardRepository.Verify(x => x.AddAsync(It.IsAny<EnvelopePaymentCard>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FreezeCardAsync_UpdatesStatus()
    {
        var now = new DateTimeOffset(2026, 3, 6, 20, 0, 0, TimeSpan.Zero);
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var envelopeRepository = new Mock<IEnvelopeRepository>(MockBehavior.Strict);
        var envelopeFinancialAccountRepository = new Mock<IEnvelopeFinancialAccountRepository>(MockBehavior.Strict);

        var card = new EnvelopePaymentCard(
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
            now);

        var envelopePaymentCardRepository = new Mock<IEnvelopePaymentCardRepository>();
        envelopePaymentCardRepository
            .Setup(x => x.GetByIdForUpdateAsync(cardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(card);
        envelopePaymentCardRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var envelopePaymentCardShipmentRepository = new Mock<IEnvelopePaymentCardShipmentRepository>();

        var stripeGateway = new Mock<IStripeGateway>();
        stripeGateway
            .Setup(x => x.UpdateCardStatusAsync("card_123", "Inactive", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(2));

        var service = new EnvelopePaymentCardService(
            envelopeRepository.Object,
            envelopeFinancialAccountRepository.Object,
            envelopePaymentCardRepository.Object,
            envelopePaymentCardShipmentRepository.Object,
            stripeGateway.Object,
            clock.Object);

        var result = await service.FreezeCardAsync(familyId, envelopeId, cardId);

        Assert.Equal("Inactive", result.Status);
        envelopePaymentCardRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IssuePhysicalCardAsync_CreatesCardAndShipment()
    {
        var now = new DateTimeOffset(2026, 3, 6, 20, 0, 0, TimeSpan.Zero);
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelopeFinancialAccountId = Guid.NewGuid();

        var envelopeRepository = new Mock<IEnvelopeRepository>();
        envelopeRepository
            .Setup(x => x.GetByIdAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Envelope(envelopeId, familyId, "Groceries", Money.FromDecimal(100m), Money.FromDecimal(25m)));

        var envelopeFinancialAccountRepository = new Mock<IEnvelopeFinancialAccountRepository>();
        envelopeFinancialAccountRepository
            .Setup(x => x.GetByEnvelopeIdAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnvelopeFinancialAccount(
                envelopeFinancialAccountId,
                familyId,
                envelopeId,
                "Stripe",
                "fa_123",
                now,
                now));

        var envelopePaymentCardRepository = new Mock<IEnvelopePaymentCardRepository>();
        envelopePaymentCardRepository
            .Setup(x => x.AddAsync(It.IsAny<EnvelopePaymentCard>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var envelopePaymentCardShipmentRepository = new Mock<IEnvelopePaymentCardShipmentRepository>();
        envelopePaymentCardShipmentRepository
            .Setup(x => x.AddAsync(It.IsAny<EnvelopePaymentCardShipment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var stripeGateway = new Mock<IStripeGateway>();
        stripeGateway
            .Setup(x => x.CreatePhysicalCardAsync(
                "fa_123",
                familyId,
                envelopeId,
                "Kid Card",
                It.IsAny<StripeCardShippingAddress>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StripeCardIssuanceResult(
                "card_123",
                "Active",
                "Visa",
                "4242",
                "pending",
                null,
                null));

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(1));

        var service = new EnvelopePaymentCardService(
            envelopeRepository.Object,
            envelopeFinancialAccountRepository.Object,
            envelopePaymentCardRepository.Object,
            envelopePaymentCardShipmentRepository.Object,
            stripeGateway.Object,
            clock.Object);

        var result = await service.IssuePhysicalCardAsync(
            familyId,
            envelopeId,
            "Kid Card",
            "Kid Name",
            "123 Main St",
            null,
            "Austin",
            "TX",
            "78701",
            "US");

        Assert.Equal("Physical", result.Card.Type);
        Assert.Equal("pending", result.Shipment.Status);
        envelopePaymentCardRepository.Verify(x => x.AddAsync(It.IsAny<EnvelopePaymentCard>(), It.IsAny<CancellationToken>()), Times.Once);
        envelopePaymentCardShipmentRepository.Verify(x => x.AddAsync(It.IsAny<EnvelopePaymentCardShipment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshPhysicalCardIssuanceStatusAsync_UpdatesCardAndShipment()
    {
        var now = new DateTimeOffset(2026, 3, 6, 20, 0, 0, TimeSpan.Zero);
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var envelopeRepository = new Mock<IEnvelopeRepository>(MockBehavior.Strict);
        var envelopeFinancialAccountRepository = new Mock<IEnvelopeFinancialAccountRepository>(MockBehavior.Strict);

        var card = new EnvelopePaymentCard(
            cardId,
            familyId,
            envelopeId,
            Guid.NewGuid(),
            "Stripe",
            "card_123",
            "Physical",
            "Active",
            "Visa",
            "4242",
            now,
            now);

        var shipment = new EnvelopePaymentCardShipment(
            Guid.NewGuid(),
            familyId,
            envelopeId,
            cardId,
            "Kid Name",
            "123 Main St",
            null,
            "Austin",
            "TX",
            "78701",
            "US",
            "pending",
            null,
            null,
            now,
            now);

        var envelopePaymentCardRepository = new Mock<IEnvelopePaymentCardRepository>();
        envelopePaymentCardRepository
            .Setup(x => x.GetByIdForUpdateAsync(cardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(card);
        envelopePaymentCardRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var envelopePaymentCardShipmentRepository = new Mock<IEnvelopePaymentCardShipmentRepository>();
        envelopePaymentCardShipmentRepository
            .Setup(x => x.GetByCardIdForUpdateAsync(cardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shipment);

        var stripeGateway = new Mock<IStripeGateway>();
        stripeGateway
            .Setup(x => x.GetCardStatusAsync("card_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StripeCardStatusResult(
                "Active",
                "shipped",
                "UPS",
                "1Z123"));

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(3));

        var service = new EnvelopePaymentCardService(
            envelopeRepository.Object,
            envelopeFinancialAccountRepository.Object,
            envelopePaymentCardRepository.Object,
            envelopePaymentCardShipmentRepository.Object,
            stripeGateway.Object,
            clock.Object);

        var result = await service.RefreshPhysicalCardIssuanceStatusAsync(
            familyId,
            envelopeId,
            cardId);

        Assert.Equal("shipped", result.Shipment.Status);
        Assert.Equal("UPS", result.Shipment.Carrier);
        envelopePaymentCardRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
