using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public sealed class PlaidBalanceReconciliationServiceTests
{
    [Fact]
    public async Task RefreshFamilyBalancesAsync_UpdatesBalance_AndPersistsSnapshot()
    {
        var now = new DateTimeOffset(2026, 3, 6, 22, 0, 0, TimeSpan.Zero);
        var familyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var profileRepository = new Mock<IFamilyFinancialProfileRepository>();
        profileRepository
            .Setup(x => x.GetByFamilyIdAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FamilyFinancialProfile(
                Guid.NewGuid(),
                familyId,
                "item_123",
                "access_123",
                null,
                null,
                now,
                now));

        var account = new Account(
            accountId,
            familyId,
            "Checking",
            AccountType.Checking,
            Money.FromDecimal(100m));
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(x => x.GetByIdForUpdateAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        accountRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var accountLinkRepository = new Mock<IPlaidAccountLinkRepository>();
        accountLinkRepository
            .Setup(x => x.ListByFamilyAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PlaidAccountLink(
                    Guid.NewGuid(),
                    familyId,
                    accountId,
                    "plaid_acct_1",
                    now,
                    now)
            ]);

        IReadOnlyCollection<PlaidBalanceSnapshot>? snapshots = null;
        var snapshotRepository = new Mock<IPlaidBalanceSnapshotRepository>();
        snapshotRepository
            .Setup(x => x.AddRangeAsync(It.IsAny<IReadOnlyCollection<PlaidBalanceSnapshot>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<PlaidBalanceSnapshot>, CancellationToken>((value, _) => snapshots = value)
            .Returns(Task.CompletedTask);

        var plaidGateway = new Mock<IPlaidGateway>();
        plaidGateway
            .Setup(x => x.GetAccountBalancesAsync("access_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PlaidAccountBalanceRecord("plaid_acct_1", 150m)
            ]);

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(1));

        var service = new PlaidBalanceReconciliationService(
            profileRepository.Object,
            accountRepository.Object,
            accountLinkRepository.Object,
            snapshotRepository.Object,
            plaidGateway.Object,
            clock.Object,
            Mock.Of<ILogger<PlaidBalanceReconciliationService>>());

        var result = await service.RefreshFamilyBalancesAsync(familyId);

        Assert.Equal(1, result.RefreshedCount);
        Assert.Equal(1, result.DriftedCount);
        Assert.Equal(50m, result.TotalAbsoluteDrift);
        Assert.Equal(150m, account.Balance.Amount);
        Assert.NotNull(snapshots);
        Assert.Single(snapshots!);
    }

    [Fact]
    public async Task GetReconciliationReportAsync_ReturnsDriftFlags()
    {
        var now = new DateTimeOffset(2026, 3, 6, 22, 0, 0, TimeSpan.Zero);
        var familyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var profileRepository = new Mock<IFamilyFinancialProfileRepository>();
        profileRepository
            .Setup(x => x.GetByFamilyIdAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FamilyFinancialProfile(
                Guid.NewGuid(),
                familyId,
                "item_123",
                "access_123",
                null,
                null,
                now,
                now));

        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(x => x.ListAccountsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Account(accountId, familyId, "Checking", AccountType.Checking, Money.FromDecimal(100m))
            ]);

        var accountLinkRepository = new Mock<IPlaidAccountLinkRepository>();
        accountLinkRepository
            .Setup(x => x.ListByFamilyAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PlaidAccountLink(
                    Guid.NewGuid(),
                    familyId,
                    accountId,
                    "plaid_acct_1",
                    now,
                    now)
            ]);

        var snapshotRepository = new Mock<IPlaidBalanceSnapshotRepository>(MockBehavior.Strict);
        var plaidGateway = new Mock<IPlaidGateway>();
        plaidGateway
            .Setup(x => x.GetAccountBalancesAsync("access_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PlaidAccountBalanceRecord("plaid_acct_1", 90m)
            ]);

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(1));

        var service = new PlaidBalanceReconciliationService(
            profileRepository.Object,
            accountRepository.Object,
            accountLinkRepository.Object,
            snapshotRepository.Object,
            plaidGateway.Object,
            clock.Object,
            Mock.Of<ILogger<PlaidBalanceReconciliationService>>());

        var report = await service.GetReconciliationReportAsync(familyId);

        Assert.Single(report.Accounts);
        Assert.Equal(-10m, report.Accounts[0].DriftAmount);
        Assert.True(report.Accounts[0].IsDrifted);
    }
}
