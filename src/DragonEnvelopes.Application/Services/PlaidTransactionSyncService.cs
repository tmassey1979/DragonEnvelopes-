using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DragonEnvelopes.Application.Services;

public sealed class PlaidTransactionSyncService(
    IFamilyFinancialProfileRepository familyFinancialProfileRepository,
    ITransactionRepository transactionRepository,
    IPlaidAccountLinkRepository plaidAccountLinkRepository,
    IPlaidSyncedTransactionRepository plaidSyncedTransactionRepository,
    IPlaidSyncCursorRepository plaidSyncCursorRepository,
    IPlaidGateway plaidGateway,
    IClock clock,
    ILogger<PlaidTransactionSyncService> logger) : IPlaidTransactionSyncService
{
    public async Task<PlaidAccountLinkDetails> UpsertAccountLinkAsync(
        Guid familyId,
        Guid accountId,
        string plaidAccountId,
        CancellationToken cancellationToken = default)
    {
        if (!await transactionRepository.AccountBelongsToFamilyAsync(accountId, familyId, cancellationToken))
        {
            throw new DomainValidationException("Account does not belong to requested family.");
        }

        var existing = await plaidAccountLinkRepository.GetByFamilyAndPlaidAccountIdForUpdateAsync(
            familyId,
            plaidAccountId,
            cancellationToken);
        if (existing is null)
        {
            var now = clock.UtcNow;
            var created = new PlaidAccountLink(
                Guid.NewGuid(),
                familyId,
                accountId,
                plaidAccountId,
                now,
                now);
            await plaidAccountLinkRepository.AddAsync(created, cancellationToken);
            await plaidAccountLinkRepository.SaveChangesAsync(cancellationToken);
            return Map(created);
        }

        existing.Rebind(accountId, plaidAccountId, clock.UtcNow);
        await plaidAccountLinkRepository.SaveChangesAsync(cancellationToken);
        return Map(existing);
    }

    public async Task<IReadOnlyList<PlaidAccountLinkDetails>> ListAccountLinksAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        var links = await plaidAccountLinkRepository.ListByFamilyAsync(familyId, cancellationToken);
        return links.Select(Map).ToArray();
    }

    public async Task<PlaidTransactionSyncDetails> SyncFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        var profile = await familyFinancialProfileRepository.GetByFamilyIdAsync(familyId, cancellationToken)
            ?? throw new DomainValidationException("Family financial profile was not found.");
        if (!profile.PlaidConnected || string.IsNullOrWhiteSpace(profile.PlaidAccessToken))
        {
            throw new DomainValidationException("Plaid is not connected for this family.");
        }

        var cursorState = await plaidSyncCursorRepository.GetByFamilyIdForUpdateAsync(familyId, cancellationToken);
        var cursor = cursorState?.Cursor;

        var pulledCount = 0;
        var insertedCount = 0;
        var dedupedCount = 0;
        var unmappedCount = 0;

        var hasMore = true;
        while (hasMore)
        {
            var batch = await plaidGateway.SyncTransactionsAsync(
                profile.PlaidAccessToken,
                cursor,
                count: 100,
                cancellationToken);

            var candidates = batch.Added.Concat(batch.Modified).ToArray();
            pulledCount += candidates.Length;

            var newTransactions = new List<Transaction>();
            var syncLinks = new List<PlaidSyncedTransaction>();
            foreach (var candidate in candidates)
            {
                if (await plaidSyncedTransactionRepository.ExistsAsync(familyId, candidate.PlaidTransactionId, cancellationToken))
                {
                    dedupedCount += 1;
                    continue;
                }

                var accountLink = await plaidAccountLinkRepository.GetByFamilyAndPlaidAccountIdAsync(
                    familyId,
                    candidate.PlaidAccountId,
                    cancellationToken);
                if (accountLink is null)
                {
                    unmappedCount += 1;
                    continue;
                }

                var localAmount = -candidate.Amount;
                if (localAmount == 0m)
                {
                    dedupedCount += 1;
                    continue;
                }

                var transaction = new Transaction(
                    Guid.NewGuid(),
                    accountLink.AccountId,
                    Money.FromDecimal(localAmount),
                    candidate.Description,
                    candidate.Merchant,
                    candidate.OccurredAtUtc,
                    category: null,
                    envelopeId: null);
                newTransactions.Add(transaction);

                syncLinks.Add(new PlaidSyncedTransaction(
                    Guid.NewGuid(),
                    familyId,
                    candidate.PlaidTransactionId,
                    accountLink.AccountId,
                    transaction.Id,
                    candidate.OccurredAtUtc,
                    clock.UtcNow));
            }

            if (newTransactions.Count > 0)
            {
                await plaidSyncedTransactionRepository.AddRangeAsync(syncLinks, cancellationToken);
                await transactionRepository.AddTransactionsAsync(newTransactions, cancellationToken);
                insertedCount += newTransactions.Count;
            }

            cursor = batch.NextCursor;
            hasMore = batch.HasMore;

            logger.LogInformation(
                "Plaid transaction sync batch processed. FamilyId={FamilyId}, Pulled={PulledCount}, Inserted={InsertedCount}, Deduped={DedupedCount}, Unmapped={UnmappedCount}, HasMore={HasMore}",
                familyId,
                pulledCount,
                insertedCount,
                dedupedCount,
                unmappedCount,
                hasMore);
        }

        if (cursorState is null)
        {
            await plaidSyncCursorRepository.AddAsync(
                new PlaidSyncCursor(
                    Guid.NewGuid(),
                    familyId,
                    cursor,
                    clock.UtcNow),
                cancellationToken);
        }
        else
        {
            cursorState.Update(cursor, clock.UtcNow);
        }

        await plaidSyncCursorRepository.SaveChangesAsync(cancellationToken);
        return new PlaidTransactionSyncDetails(
            familyId,
            pulledCount,
            insertedCount,
            dedupedCount,
            unmappedCount,
            cursor,
            clock.UtcNow);
    }

    public async Task<IReadOnlyList<PlaidTransactionSyncDetails>> SyncConnectedFamiliesAsync(
        CancellationToken cancellationToken = default)
    {
        var profiles = await familyFinancialProfileRepository.ListPlaidConnectedAsync(cancellationToken);
        var results = new List<PlaidTransactionSyncDetails>(profiles.Count);
        foreach (var profile in profiles)
        {
            try
            {
                var result = await SyncFamilyAsync(profile.FamilyId, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Plaid sync failed for family {FamilyId}.", profile.FamilyId);
            }
        }

        return results;
    }

    private static PlaidAccountLinkDetails Map(PlaidAccountLink link)
    {
        return new PlaidAccountLinkDetails(
            link.Id,
            link.FamilyId,
            link.AccountId,
            link.PlaidAccountId,
            link.CreatedAtUtc,
            link.UpdatedAtUtc);
    }
}
