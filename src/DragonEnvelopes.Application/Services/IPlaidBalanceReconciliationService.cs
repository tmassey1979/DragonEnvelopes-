using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IPlaidBalanceReconciliationService
{
    Task<PlaidBalanceRefreshDetails> RefreshFamilyBalancesAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<PlaidReconciliationReportDetails> GetReconciliationReportAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlaidBalanceRefreshDetails>> RefreshConnectedFamiliesAsync(
        CancellationToken cancellationToken = default);
}
