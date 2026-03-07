using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;

namespace DragonEnvelopes.Application.Services;

public sealed class ReportingService(
    IReportingRepository reportingRepository,
    IBudgetService budgetService) : IReportingService
{
    public Task<IReadOnlyList<EnvelopeBalanceReportDetails>> GetEnvelopeBalancesAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return reportingRepository.ListEnvelopeBalancesAsync(familyId, cancellationToken);
    }

    public async Task<IReadOnlyList<MonthlySpendReportPointDetails>> GetMonthlySpendAsync(
        Guid familyId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toInclusive,
        CancellationToken cancellationToken = default)
    {
        var rows = await reportingRepository.ListTransactionsAsync(
            familyId,
            fromInclusive,
            toInclusive,
            cancellationToken);

        return rows
            .Where(static row => row.Amount < 0m && !row.TransferId.HasValue)
            .GroupBy(static row => $"{row.OccurredAt:yyyy-MM}")
            .Select(static group => new MonthlySpendReportPointDetails(
                group.Key,
                decimal.Round(group.Sum(static row => Math.Abs(row.Amount)), 2, MidpointRounding.AwayFromZero)))
            .OrderBy(static point => point.Month)
            .ToArray();
    }

    public async Task<IReadOnlyList<CategoryBreakdownReportItemDetails>> GetCategoryBreakdownAsync(
        Guid familyId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toInclusive,
        CancellationToken cancellationToken = default)
    {
        var rows = await reportingRepository.ListTransactionsAsync(
            familyId,
            fromInclusive,
            toInclusive,
            cancellationToken);

        return rows
            .Where(static row => row.Amount < 0m && !row.TransferId.HasValue)
            .GroupBy(static row => string.IsNullOrWhiteSpace(row.Category) ? "Uncategorized" : row.Category!.Trim())
            .Select(static group => new CategoryBreakdownReportItemDetails(
                group.Key,
                decimal.Round(group.Sum(static row => Math.Abs(row.Amount)), 2, MidpointRounding.AwayFromZero)))
            .OrderByDescending(static item => item.TotalSpend)
            .ThenBy(static item => item.Category)
            .ToArray();
    }

    public Task<BudgetDetails?> GetRemainingBudgetAsync(
        Guid familyId,
        string month,
        CancellationToken cancellationToken = default)
    {
        return budgetService.GetByMonthAsync(familyId, month, cancellationToken);
    }
}
