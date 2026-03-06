using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DragonEnvelopes.Application.Services;

public sealed class PlaidBalanceReconciliationService(
    IFamilyFinancialProfileRepository familyFinancialProfileRepository,
    IAccountRepository accountRepository,
    IPlaidAccountLinkRepository plaidAccountLinkRepository,
    IPlaidBalanceSnapshotRepository plaidBalanceSnapshotRepository,
    IPlaidGateway plaidGateway,
    IClock clock,
    ILogger<PlaidBalanceReconciliationService> logger) : IPlaidBalanceReconciliationService
{
    public async Task<PlaidBalanceRefreshDetails> RefreshFamilyBalancesAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        var profile = await familyFinancialProfileRepository.GetByFamilyIdAsync(familyId, cancellationToken)
            ?? throw new DomainValidationException("Family financial profile was not found.");
        if (!profile.PlaidConnected || string.IsNullOrWhiteSpace(profile.PlaidAccessToken))
        {
            throw new DomainValidationException("Plaid is not connected for this family.");
        }

        var links = await plaidAccountLinkRepository.ListByFamilyAsync(familyId, cancellationToken);
        if (links.Count == 0)
        {
            return new PlaidBalanceRefreshDetails(familyId, 0, 0, 0m, clock.UtcNow);
        }

        var providerBalances = (await plaidGateway.GetAccountBalancesAsync(profile.PlaidAccessToken, cancellationToken))
            .ToDictionary(static item => item.PlaidAccountId, static item => item.CurrentBalance, StringComparer.OrdinalIgnoreCase);

        var snapshots = new List<PlaidBalanceSnapshot>();
        var refreshedCount = 0;
        var driftedCount = 0;
        var totalDrift = 0m;
        var now = clock.UtcNow;

        foreach (var link in links)
        {
            if (!providerBalances.TryGetValue(link.PlaidAccountId, out var providerBalance))
            {
                continue;
            }

            var account = await accountRepository.GetByIdForUpdateAsync(link.AccountId, cancellationToken);
            if (account is null || account.FamilyId != familyId)
            {
                continue;
            }

            var providerNormalized = Math.Max(providerBalance, 0m);
            var internalBefore = account.Balance.Amount;
            var drift = providerNormalized - internalBefore;
            if (drift > 0m)
            {
                account.Deposit(Money.FromDecimal(drift));
            }
            else if (drift < 0m)
            {
                account.Withdraw(Money.FromDecimal(decimal.Abs(drift)));
            }

            if (drift != 0m)
            {
                driftedCount += 1;
                totalDrift += decimal.Abs(drift);
            }

            refreshedCount += 1;
            snapshots.Add(new PlaidBalanceSnapshot(
                Guid.NewGuid(),
                familyId,
                account.Id,
                link.PlaidAccountId,
                internalBefore,
                providerNormalized,
                account.Balance.Amount,
                drift,
                now));
        }

        if (snapshots.Count > 0)
        {
            await plaidBalanceSnapshotRepository.AddRangeAsync(snapshots, cancellationToken);
            await accountRepository.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "Plaid balance refresh completed. FamilyId={FamilyId}, Refreshed={RefreshedCount}, Drifted={DriftedCount}, TotalAbsDrift={TotalAbsoluteDrift}",
            familyId,
            refreshedCount,
            driftedCount,
            totalDrift);

        return new PlaidBalanceRefreshDetails(
            familyId,
            refreshedCount,
            driftedCount,
            totalDrift,
            now);
    }

    public async Task<PlaidReconciliationReportDetails> GetReconciliationReportAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        var profile = await familyFinancialProfileRepository.GetByFamilyIdAsync(familyId, cancellationToken)
            ?? throw new DomainValidationException("Family financial profile was not found.");
        if (!profile.PlaidConnected || string.IsNullOrWhiteSpace(profile.PlaidAccessToken))
        {
            throw new DomainValidationException("Plaid is not connected for this family.");
        }

        var links = await plaidAccountLinkRepository.ListByFamilyAsync(familyId, cancellationToken);
        var accounts = await accountRepository.ListAccountsAsync(familyId, cancellationToken);
        var accountById = accounts.ToDictionary(static account => account.Id);
        var providerBalances = (await plaidGateway.GetAccountBalancesAsync(profile.PlaidAccessToken, cancellationToken))
            .ToDictionary(static item => item.PlaidAccountId, static item => item.CurrentBalance, StringComparer.OrdinalIgnoreCase);

        var drift = new List<PlaidAccountDriftDetails>();
        foreach (var link in links)
        {
            if (!accountById.TryGetValue(link.AccountId, out var account))
            {
                continue;
            }

            if (!providerBalances.TryGetValue(link.PlaidAccountId, out var providerBalance))
            {
                continue;
            }

            var providerNormalized = Math.Max(providerBalance, 0m);
            var internalBalance = account.Balance.Amount;
            var driftAmount = providerNormalized - internalBalance;
            drift.Add(new PlaidAccountDriftDetails(
                account.Id,
                account.Name,
                link.PlaidAccountId,
                internalBalance,
                providerNormalized,
                driftAmount,
                driftAmount != 0m));
        }

        logger.LogInformation(
            "Plaid reconciliation report generated. FamilyId={FamilyId}, AccountCount={AccountCount}, Drifted={DriftedCount}",
            familyId,
            drift.Count,
            drift.Count(static account => account.IsDrifted));

        return new PlaidReconciliationReportDetails(
            familyId,
            clock.UtcNow,
            drift);
    }

    public async Task<IReadOnlyList<PlaidBalanceRefreshDetails>> RefreshConnectedFamiliesAsync(
        CancellationToken cancellationToken = default)
    {
        var profiles = await familyFinancialProfileRepository.ListPlaidConnectedAsync(cancellationToken);
        var results = new List<PlaidBalanceRefreshDetails>(profiles.Count);
        foreach (var profile in profiles)
        {
            try
            {
                var result = await RefreshFamilyBalancesAsync(profile.FamilyId, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Plaid balance refresh failed for family {FamilyId}.", profile.FamilyId);
            }
        }

        return results;
    }
}
