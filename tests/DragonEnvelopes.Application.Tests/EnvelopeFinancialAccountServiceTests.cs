using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public sealed class EnvelopeFinancialAccountServiceTests
{
    [Fact]
    public async Task LinkStripeFinancialAccountAsync_ReturnsExisting_WhenAlreadyLinked()
    {
        var now = new DateTimeOffset(2026, 3, 6, 18, 0, 0, TimeSpan.Zero);
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelope = new Envelope(envelopeId, familyId, "Groceries", Money.FromDecimal(100m), Money.FromDecimal(20m));

        var existing = new EnvelopeFinancialAccount(
            Guid.NewGuid(),
            familyId,
            envelopeId,
            "Stripe",
            "fa_existing",
            now,
            now);

        var envelopeRepository = new Mock<IEnvelopeRepository>();
        envelopeRepository
            .Setup(x => x.GetByIdAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);

        var financialAccountRepository = new Mock<IEnvelopeFinancialAccountRepository>();
        financialAccountRepository
            .Setup(x => x.GetByEnvelopeIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var financialProfileRepository = new Mock<IFamilyFinancialProfileRepository>();
        financialProfileRepository
            .Setup(x => x.GetByFamilyIdForUpdateAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FamilyFinancialProfile(
                Guid.NewGuid(),
                familyId,
                plaidItemId: null,
                plaidAccessToken: null,
                stripeCustomerId: "cus_test",
                stripeDefaultPaymentMethodId: null,
                createdAtUtc: now,
                updatedAtUtc: now));

        var stripeGateway = new Mock<IStripeGateway>(MockBehavior.Strict);
        var clock = new Mock<IClock>(MockBehavior.Strict);

        var service = new EnvelopeFinancialAccountService(
            envelopeRepository.Object,
            financialAccountRepository.Object,
            financialProfileRepository.Object,
            stripeGateway.Object,
            clock.Object);

        var result = await service.LinkStripeFinancialAccountAsync(familyId, envelopeId, null);

        Assert.Equal(existing.Id, result.Id);
        Assert.Equal("fa_existing", result.ProviderFinancialAccountId);
        financialAccountRepository.Verify(x => x.AddAsync(It.IsAny<EnvelopeFinancialAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkStripeFinancialAccountAsync_CreatesNew_WhenMissing()
    {
        var now = new DateTimeOffset(2026, 3, 6, 18, 0, 0, TimeSpan.Zero);
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelope = new Envelope(envelopeId, familyId, "Groceries", Money.FromDecimal(100m), Money.FromDecimal(20m));

        var envelopeRepository = new Mock<IEnvelopeRepository>();
        envelopeRepository
            .Setup(x => x.GetByIdAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);

        var financialAccountRepository = new Mock<IEnvelopeFinancialAccountRepository>();
        financialAccountRepository
            .Setup(x => x.GetByEnvelopeIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnvelopeFinancialAccount?)null);
        financialAccountRepository
            .Setup(x => x.AddAsync(It.IsAny<EnvelopeFinancialAccount>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        financialAccountRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var financialProfileRepository = new Mock<IFamilyFinancialProfileRepository>();
        financialProfileRepository
            .Setup(x => x.GetByFamilyIdForUpdateAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FamilyFinancialProfile(
                Guid.NewGuid(),
                familyId,
                plaidItemId: null,
                plaidAccessToken: null,
                stripeCustomerId: "cus_test",
                stripeDefaultPaymentMethodId: null,
                createdAtUtc: now,
                updatedAtUtc: now));

        var stripeGateway = new Mock<IStripeGateway>();
        stripeGateway
            .Setup(x => x.CreateFinancialAccountAsync("cus_test", familyId, envelopeId, "Groceries", It.IsAny<CancellationToken>()))
            .ReturnsAsync("fa_new");

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(1));

        var service = new EnvelopeFinancialAccountService(
            envelopeRepository.Object,
            financialAccountRepository.Object,
            financialProfileRepository.Object,
            stripeGateway.Object,
            clock.Object);

        var result = await service.LinkStripeFinancialAccountAsync(familyId, envelopeId, null);

        Assert.Equal("Stripe", result.Provider);
        Assert.Equal("fa_new", result.ProviderFinancialAccountId);
        financialAccountRepository.Verify(x => x.AddAsync(It.IsAny<EnvelopeFinancialAccount>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LinkStripeFinancialAccountAsync_Throws_WhenStripeCustomerMissing()
    {
        var now = new DateTimeOffset(2026, 3, 6, 18, 0, 0, TimeSpan.Zero);
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelope = new Envelope(envelopeId, familyId, "Groceries", Money.FromDecimal(100m), Money.FromDecimal(20m));

        var envelopeRepository = new Mock<IEnvelopeRepository>();
        envelopeRepository
            .Setup(x => x.GetByIdAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);

        var financialAccountRepository = new Mock<IEnvelopeFinancialAccountRepository>();
        financialAccountRepository
            .Setup(x => x.GetByEnvelopeIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnvelopeFinancialAccount?)null);

        var financialProfileRepository = new Mock<IFamilyFinancialProfileRepository>();
        financialProfileRepository
            .Setup(x => x.GetByFamilyIdForUpdateAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FamilyFinancialProfile(
                Guid.NewGuid(),
                familyId,
                plaidItemId: null,
                plaidAccessToken: null,
                stripeCustomerId: null,
                stripeDefaultPaymentMethodId: null,
                createdAtUtc: now,
                updatedAtUtc: now));

        var stripeGateway = new Mock<IStripeGateway>(MockBehavior.Strict);
        var clock = new Mock<IClock>(MockBehavior.Strict);

        var service = new EnvelopeFinancialAccountService(
            envelopeRepository.Object,
            financialAccountRepository.Object,
            financialProfileRepository.Object,
            stripeGateway.Object,
            clock.Object);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            service.LinkStripeFinancialAccountAsync(familyId, envelopeId, null));
    }

    [Fact]
    public async Task LinkStripeFinancialAccountAsync_EnqueuesFinancialOutboxMessage_WhenCreated()
    {
        var now = new DateTimeOffset(2026, 3, 6, 18, 0, 0, TimeSpan.Zero);
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelope = new Envelope(envelopeId, familyId, "Groceries", Money.FromDecimal(100m), Money.FromDecimal(20m));

        var envelopeRepository = new Mock<IEnvelopeRepository>();
        envelopeRepository
            .Setup(x => x.GetByIdAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);

        var financialAccountRepository = new Mock<IEnvelopeFinancialAccountRepository>();
        financialAccountRepository
            .Setup(x => x.GetByEnvelopeIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnvelopeFinancialAccount?)null);
        financialAccountRepository
            .Setup(x => x.AddAsync(It.IsAny<EnvelopeFinancialAccount>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        financialAccountRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var financialProfileRepository = new Mock<IFamilyFinancialProfileRepository>();
        financialProfileRepository
            .Setup(x => x.GetByFamilyIdForUpdateAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FamilyFinancialProfile(
                Guid.NewGuid(),
                familyId,
                plaidItemId: null,
                plaidAccessToken: null,
                stripeCustomerId: "cus_test",
                stripeDefaultPaymentMethodId: null,
                createdAtUtc: now,
                updatedAtUtc: now));

        var stripeGateway = new Mock<IStripeGateway>();
        stripeGateway
            .Setup(x => x.CreateFinancialAccountAsync("cus_test", familyId, envelopeId, "Groceries", It.IsAny<CancellationToken>()))
            .ReturnsAsync("fa_new");

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(1));
        var outboxRepository = new Mock<IIntegrationOutboxRepository>();

        var service = new EnvelopeFinancialAccountService(
            envelopeRepository.Object,
            financialAccountRepository.Object,
            financialProfileRepository.Object,
            stripeGateway.Object,
            clock.Object,
            outboxRepository.Object);

        await service.LinkStripeFinancialAccountAsync(familyId, envelopeId, null);

        outboxRepository.Verify(
            x => x.AddAsync(
                It.Is<IntegrationOutboxMessage>(message =>
                    message.SourceService == "financial-api"
                    && message.RoutingKey == "financial.stripe.financial-account.provisioned.v1"
                    && message.EventName == "StripeFinancialAccountProvisioned"
                    && message.FamilyId == familyId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
