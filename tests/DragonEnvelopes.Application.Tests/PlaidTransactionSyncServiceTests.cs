using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public sealed class PlaidTransactionSyncServiceTests
{
    [Fact]
    public async Task SyncFamilyAsync_InsertsMappedTransactions_AndUpdatesCursor()
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

        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository
            .Setup(x => x.AddTransactionsAsync(It.IsAny<IReadOnlyList<Transaction>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var accountLinkRepository = new Mock<IPlaidAccountLinkRepository>();
        accountLinkRepository
            .Setup(x => x.GetByFamilyAndPlaidAccountIdAsync(familyId, "plaid_acct_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlaidAccountLink(
                Guid.NewGuid(),
                familyId,
                accountId,
                "plaid_acct_1",
                now,
                now));

        var syncedTransactionRepository = new Mock<IPlaidSyncedTransactionRepository>();
        syncedTransactionRepository
            .Setup(x => x.ExistsAsync(familyId, "plaid_txn_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        syncedTransactionRepository
            .Setup(x => x.AddRangeAsync(It.IsAny<IReadOnlyCollection<PlaidSyncedTransaction>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cursorRepository = new Mock<IPlaidSyncCursorRepository>();
        cursorRepository
            .Setup(x => x.GetByFamilyIdForUpdateAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlaidSyncCursor?)null);
        cursorRepository
            .Setup(x => x.AddAsync(It.IsAny<PlaidSyncCursor>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        cursorRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var plaidGateway = new Mock<IPlaidGateway>();
        plaidGateway
            .Setup(x => x.SyncTransactionsAsync("access_123", null, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlaidTransactionSyncResult(
                "cursor_1",
                HasMore: false,
                Added:
                [
                    new PlaidTransactionRecord(
                        "plaid_txn_1",
                        "plaid_acct_1",
                        Amount: 12.34m,
                        Description: "Store Purchase",
                        Merchant: "Store",
                        OccurredAtUtc: now)
                ],
                Modified: []));

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(1));

        var service = new PlaidTransactionSyncService(
            profileRepository.Object,
            transactionRepository.Object,
            accountLinkRepository.Object,
            syncedTransactionRepository.Object,
            cursorRepository.Object,
            plaidGateway.Object,
            clock.Object,
            Mock.Of<ILogger<PlaidTransactionSyncService>>());

        var result = await service.SyncFamilyAsync(familyId);

        Assert.Equal(1, result.PulledCount);
        Assert.Equal(1, result.InsertedCount);
        Assert.Equal(0, result.DedupedCount);
        Assert.Equal(0, result.UnmappedCount);
        Assert.Equal("cursor_1", result.NextCursor);
        transactionRepository.Verify(x => x.AddTransactionsAsync(It.IsAny<IReadOnlyList<Transaction>>(), It.IsAny<CancellationToken>()), Times.Once);
        syncedTransactionRepository.Verify(x => x.AddRangeAsync(It.IsAny<IReadOnlyCollection<PlaidSyncedTransaction>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncFamilyAsync_TracksDedupedAndUnmappedTransactions()
    {
        var now = new DateTimeOffset(2026, 3, 6, 22, 0, 0, TimeSpan.Zero);
        var familyId = Guid.NewGuid();

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

        var transactionRepository = new Mock<ITransactionRepository>(MockBehavior.Strict);
        var accountLinkRepository = new Mock<IPlaidAccountLinkRepository>();
        accountLinkRepository
            .Setup(x => x.GetByFamilyAndPlaidAccountIdAsync(familyId, "mapped_missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlaidAccountLink?)null);

        var syncedTransactionRepository = new Mock<IPlaidSyncedTransactionRepository>();
        syncedTransactionRepository
            .Setup(x => x.ExistsAsync(familyId, "plaid_txn_deduped", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        syncedTransactionRepository
            .Setup(x => x.ExistsAsync(familyId, "plaid_txn_unmapped", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var existingCursor = new PlaidSyncCursor(
            Guid.NewGuid(),
            familyId,
            "cursor_0",
            now);
        var cursorRepository = new Mock<IPlaidSyncCursorRepository>();
        cursorRepository
            .Setup(x => x.GetByFamilyIdForUpdateAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCursor);
        cursorRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var plaidGateway = new Mock<IPlaidGateway>();
        plaidGateway
            .Setup(x => x.SyncTransactionsAsync("access_123", "cursor_0", 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlaidTransactionSyncResult(
                "cursor_1",
                HasMore: false,
                Added:
                [
                    new PlaidTransactionRecord("plaid_txn_deduped", "mapped_missing", 10m, "Deduped", "Deduped", now),
                    new PlaidTransactionRecord("plaid_txn_unmapped", "mapped_missing", 20m, "Unmapped", "Unmapped", now)
                ],
                Modified: []));

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(1));

        var service = new PlaidTransactionSyncService(
            profileRepository.Object,
            transactionRepository.Object,
            accountLinkRepository.Object,
            syncedTransactionRepository.Object,
            cursorRepository.Object,
            plaidGateway.Object,
            clock.Object,
            Mock.Of<ILogger<PlaidTransactionSyncService>>());

        var result = await service.SyncFamilyAsync(familyId);

        Assert.Equal(2, result.PulledCount);
        Assert.Equal(0, result.InsertedCount);
        Assert.Equal(1, result.DedupedCount);
        Assert.Equal(1, result.UnmappedCount);
        Assert.Equal("cursor_1", existingCursor.Cursor);
    }
}
