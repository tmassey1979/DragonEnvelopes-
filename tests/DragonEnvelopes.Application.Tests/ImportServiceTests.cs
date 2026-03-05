using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public class ImportServiceTests
{
    [Fact]
    public async Task PreviewTransactionsAsync_ReportsMalformedRows()
    {
        var repository = new Mock<ITransactionRepository>();
        var familyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        repository.Setup(x => x.AccountBelongsToFamilyAsync(accountId, familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(x => x.ListTransactionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new ImportService(repository.Object, new ImportDedupService());
        const string csv = """
date,amount,merchant,description
2026-03-01,abc,Store,Groceries
,10.00,Store,Missing date
""";

        var preview = await service.PreviewTransactionsAsync(familyId, accountId, csv, null, null);

        Assert.Equal(2, preview.Parsed);
        Assert.Equal(0, preview.Valid);
        Assert.Contains(preview.Rows, row => row.Errors.Contains("Amount is invalid."));
        Assert.Contains(preview.Rows, row => row.Errors.Contains("Date is required."));
    }

    [Fact]
    public async Task PreviewTransactionsAsync_SupportsAlternateHeaderNamesViaMapping()
    {
        var repository = new Mock<ITransactionRepository>();
        var familyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        repository.Setup(x => x.AccountBelongsToFamilyAsync(accountId, familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(x => x.ListTransactionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new ImportService(repository.Object, new ImportDedupService());
        const string csv = """
posted_date,amt,payee,memo
2026-03-01,20.50,City Water,March bill
""";
        var mappings = new Dictionary<string, string>
        {
            ["occurredOn"] = "posted_date",
            ["amount"] = "amt",
            ["merchant"] = "payee",
            ["description"] = "memo"
        };

        var preview = await service.PreviewTransactionsAsync(familyId, accountId, csv, null, mappings);

        Assert.Single(preview.Rows);
        Assert.True(preview.Rows[0].Errors.Count == 0);
        Assert.Equal("City Water", preview.Rows[0].Merchant);
        Assert.Equal("March bill", preview.Rows[0].Description);
    }

    [Fact]
    public async Task PreviewAndCommit_ApplyDeterministicDedupeWithoutFalsePositive()
    {
        var repository = new Mock<ITransactionRepository>();
        var familyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        repository.Setup(x => x.AccountBelongsToFamilyAsync(accountId, familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(x => x.ListTransactionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Transaction(
                    Guid.NewGuid(),
                    accountId,
                    Money.FromDecimal(-45m),
                    "Coffee",
                    "Cafe",
                    new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
                    null,
                    null)
            ]);

        IReadOnlyList<Transaction>? inserted = null;
        repository.Setup(x => x.AddTransactionsAsync(It.IsAny<IReadOnlyList<Transaction>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Transaction>, CancellationToken>((items, _) => inserted = items)
            .Returns(Task.CompletedTask);

        var service = new ImportService(repository.Object, new ImportDedupService());
        const string csv = """
date,amount,merchant,description
2026-03-01,-45.00,Cafe,Coffee
2026-03-02,-45.00,Cafe,Coffee
""";

        var preview = await service.PreviewTransactionsAsync(familyId, accountId, csv, null, null);
        var commit = await service.CommitTransactionsAsync(familyId, accountId, csv, null, null, null);

        Assert.Equal(2, preview.Parsed);
        Assert.Equal(1, preview.Deduped);
        Assert.Single(preview.Rows.Where(static row => row.IsDuplicate));
        Assert.Equal(1, commit.Inserted);
        Assert.NotNull(inserted);
        Assert.Single(inserted!);
        Assert.Equal(new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.Zero), inserted![0].OccurredAt);
    }
}
