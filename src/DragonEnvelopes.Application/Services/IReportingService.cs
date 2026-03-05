using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IReportingService
{
    Task<IReadOnlyList<EnvelopeBalanceReportDetails>> GetEnvelopeBalancesAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlySpendReportPointDetails>> GetMonthlySpendAsync(
        Guid familyId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toInclusive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryBreakdownReportItemDetails>> GetCategoryBreakdownAsync(
        Guid familyId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toInclusive,
        CancellationToken cancellationToken = default);

    Task<BudgetDetails?> GetRemainingBudgetAsync(
        Guid familyId,
        string month,
        CancellationToken cancellationToken = default);
}
