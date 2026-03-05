using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public class ReportingServiceTests
{
    [Fact]
    public async Task GetMonthlySpendAsync_GroupsByMonthAndUsesAbsoluteSpend()
    {
        var reportingRepository = new Mock<IReportingRepository>();
        var budgetService = new Mock<IBudgetService>();
        var familyId = Guid.NewGuid();
        var from = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 4, 30, 23, 59, 59, TimeSpan.Zero);

        reportingRepository.Setup(x => x.ListTransactionsAsync(familyId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new TransactionReportRow(-10m, "Food", new DateTimeOffset(2026, 3, 5, 0, 0, 0, TimeSpan.Zero)),
                new TransactionReportRow(-15m, "Food", new DateTimeOffset(2026, 3, 8, 0, 0, 0, TimeSpan.Zero)),
                new TransactionReportRow(25m, "Income", new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero)),
                new TransactionReportRow(-20m, "Fuel", new DateTimeOffset(2026, 4, 2, 0, 0, 0, TimeSpan.Zero))
            ]);

        var service = new ReportingService(reportingRepository.Object, budgetService.Object);
        var results = await service.GetMonthlySpendAsync(familyId, from, to);

        Assert.Equal(2, results.Count);
        Assert.Equal("2026-03", results[0].Month);
        Assert.Equal(25m, results[0].TotalSpend);
        Assert.Equal("2026-04", results[1].Month);
        Assert.Equal(20m, results[1].TotalSpend);
    }

    [Fact]
    public async Task GetCategoryBreakdownAsync_GroupsAndOrdersByTotalSpend()
    {
        var reportingRepository = new Mock<IReportingRepository>();
        var budgetService = new Mock<IBudgetService>();
        var familyId = Guid.NewGuid();
        var from = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 3, 31, 23, 59, 59, TimeSpan.Zero);

        reportingRepository.Setup(x => x.ListTransactionsAsync(familyId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new TransactionReportRow(-40m, "Food", new DateTimeOffset(2026, 3, 5, 0, 0, 0, TimeSpan.Zero)),
                new TransactionReportRow(-10m, null, new DateTimeOffset(2026, 3, 8, 0, 0, 0, TimeSpan.Zero)),
                new TransactionReportRow(-15m, "Fuel", new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero))
            ]);

        var service = new ReportingService(reportingRepository.Object, budgetService.Object);
        var results = await service.GetCategoryBreakdownAsync(familyId, from, to);

        Assert.Equal(3, results.Count);
        Assert.Equal("Food", results[0].Category);
        Assert.Equal(40m, results[0].TotalSpend);
        Assert.Equal("Fuel", results[1].Category);
        Assert.Equal("Uncategorized", results[2].Category);
    }
}
