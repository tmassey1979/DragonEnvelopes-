using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public sealed class FinancialIntegrationServiceTests
{
    [Fact]
    public async Task GetStatusAsync_ReturnsDisconnected_WhenProfileMissing()
    {
        var familyId = Guid.NewGuid();
        var repository = new Mock<IFamilyFinancialProfileRepository>();
        repository
            .Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository
            .Setup(x => x.GetByFamilyIdAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FamilyFinancialProfile?)null);

        var plaidGateway = new Mock<IPlaidGateway>(MockBehavior.Strict);
        var stripeGateway = new Mock<IStripeGateway>(MockBehavior.Strict);
        var clock = new Mock<IClock>(MockBehavior.Strict);

        var service = new FinancialIntegrationService(
            repository.Object,
            plaidGateway.Object,
            stripeGateway.Object,
            clock.Object);

        var status = await service.GetStatusAsync(familyId);

        Assert.Equal(familyId, status.FamilyId);
        Assert.False(status.PlaidConnected);
        Assert.False(status.StripeConnected);
        Assert.Null(status.PlaidItemId);
        Assert.Null(status.StripeCustomerId);
    }

    [Fact]
    public async Task ExchangePlaidPublicTokenAsync_UpdatesProfile_AndSaves()
    {
        var familyId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 3, 6, 12, 0, 0, TimeSpan.Zero);
        var profile = new FamilyFinancialProfile(
            Guid.NewGuid(),
            familyId,
            plaidItemId: null,
            plaidAccessToken: null,
            stripeCustomerId: null,
            stripeDefaultPaymentMethodId: null,
            createdAtUtc: now,
            updatedAtUtc: now);

        var repository = new Mock<IFamilyFinancialProfileRepository>();
        repository
            .Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository
            .Setup(x => x.GetByFamilyIdForUpdateAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        repository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var plaidGateway = new Mock<IPlaidGateway>();
        plaidGateway
            .Setup(x => x.ExchangePublicTokenAsync("public-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("item-test", "access-test"));

        var stripeGateway = new Mock<IStripeGateway>(MockBehavior.Strict);
        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(5));

        var service = new FinancialIntegrationService(
            repository.Object,
            plaidGateway.Object,
            stripeGateway.Object,
            clock.Object);

        var status = await service.ExchangePlaidPublicTokenAsync(familyId, "public-token");

        Assert.True(status.PlaidConnected);
        Assert.Equal("item-test", status.PlaidItemId);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateStripeSetupIntentAsync_CreatesCustomer_WhenMissing()
    {
        var familyId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 3, 6, 12, 0, 0, TimeSpan.Zero);
        var profile = new FamilyFinancialProfile(
            Guid.NewGuid(),
            familyId,
            plaidItemId: null,
            plaidAccessToken: null,
            stripeCustomerId: null,
            stripeDefaultPaymentMethodId: null,
            createdAtUtc: now,
            updatedAtUtc: now);

        var repository = new Mock<IFamilyFinancialProfileRepository>();
        repository
            .Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository
            .Setup(x => x.GetByFamilyIdForUpdateAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        repository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var plaidGateway = new Mock<IPlaidGateway>(MockBehavior.Strict);

        var stripeGateway = new Mock<IStripeGateway>();
        stripeGateway
            .Setup(x => x.CreateCustomerAsync(familyId, "guardian@test.dev", "Guardian", It.IsAny<CancellationToken>()))
            .ReturnsAsync("cus_test");
        stripeGateway
            .Setup(x => x.CreateSetupIntentAsync("cus_test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("seti_test", "seti_secret"));

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(10));

        var service = new FinancialIntegrationService(
            repository.Object,
            plaidGateway.Object,
            stripeGateway.Object,
            clock.Object);

        var setupIntent = await service.CreateStripeSetupIntentAsync(
            familyId,
            "guardian@test.dev",
            "Guardian");

        Assert.Equal("cus_test", setupIntent.CustomerId);
        Assert.Equal("seti_test", setupIntent.SetupIntentId);
        Assert.Equal("seti_secret", setupIntent.ClientSecret);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
