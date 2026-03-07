using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class TransactionService(
    ITransactionRepository transactionRepository,
    IEnvelopeRepository envelopeRepository,
    ICategorizationRuleEngine categorizationRuleEngine,
    IIncomeAllocationEngine incomeAllocationEngine) : ITransactionService
{
    public async Task<TransactionDetails> CreateAsync(
        Guid accountId,
        decimal amount,
        string description,
        string merchant,
        DateTimeOffset occurredAt,
        string? category,
        Guid? envelopeId,
        bool hasSplits,
        IReadOnlyList<TransactionSplitCreateDetails>? splits,
        CancellationToken cancellationToken = default)
    {
        var familyId = await transactionRepository.GetAccountFamilyIdAsync(accountId, cancellationToken);
        if (!familyId.HasValue)
        {
            throw new DomainValidationException("Account was not found.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            category = await categorizationRuleEngine.EvaluateAsync(
                familyId.Value,
                description,
                merchant,
                amount,
                category,
                cancellationToken);
        }

        var hasManualSplitItems = hasSplits && splits is { Count: > 0 };
        var splitInputs = hasManualSplitItems
            ? splits!.ToArray()
            : Array.Empty<TransactionSplitCreateDetails>();
        var usedAutomaticAllocation = false;

        if (!hasManualSplitItems && !envelopeId.HasValue && amount > 0m)
        {
            splitInputs = (await incomeAllocationEngine.AllocateAsync(
                    familyId.Value,
                    description,
                    merchant,
                    amount,
                    category,
                    cancellationToken))
                .ToArray();
            usedAutomaticAllocation = splitInputs.Length > 0;
        }

        var hasSplitItems = splitInputs.Length > 0;
        Envelope? envelope = null;
        if (envelopeId.HasValue && !hasManualSplitItems)
        {
            envelope = await envelopeRepository.GetByIdForUpdateAsync(envelopeId.Value, cancellationToken);
            if (envelope is null)
            {
                throw new DomainValidationException("Envelope was not found.");
            }
        }

        var splitEntries = new List<TransactionSplitEntry>();
        Transaction transaction;
        if (hasSplitItems)
        {
            if (usedAutomaticAllocation)
            {
                transaction = new Transaction(
                    Guid.NewGuid(),
                    accountId,
                    Money.FromDecimal(amount),
                    description,
                    merchant,
                    occurredAt,
                    category,
                    envelopeId: null);
            }
            else
            {
                var splitValueObjects = splitInputs
                    .Select(static split => new TransactionSplit(
                        split.EnvelopeId,
                        Money.FromDecimal(split.Amount),
                        split.Category))
                    .ToArray();

                transaction = Transaction.CreateWithSplits(
                    Guid.NewGuid(),
                    accountId,
                    Money.FromDecimal(amount),
                    description,
                    merchant,
                    occurredAt,
                    splitValueObjects,
                    category);
            }

            foreach (var split in splitInputs)
            {
                var splitEnvelope = await envelopeRepository.GetByIdForUpdateAsync(split.EnvelopeId, cancellationToken);
                if (splitEnvelope is null)
                {
                    throw new DomainValidationException($"Envelope was not found for split id {split.EnvelopeId}.");
                }

                var splitAmount = Money.FromDecimal(Math.Abs(split.Amount));
                if (split.Amount < 0m)
                {
                    splitEnvelope.Spend(splitAmount, occurredAt);
                }
                else
                {
                    splitEnvelope.Allocate(splitAmount, occurredAt);
                }

                splitEntries.Add(new TransactionSplitEntry(
                    Guid.NewGuid(),
                    transaction.Id,
                    split.EnvelopeId,
                    Money.FromDecimal(split.Amount),
                    split.Category,
                    split.Notes));
            }
        }
        else
        {
            transaction = new Transaction(
                Guid.NewGuid(),
                accountId,
                Money.FromDecimal(amount),
                description,
                merchant,
                occurredAt,
                category,
                envelopeId);
        }

        if (envelope is not null)
        {
            var transactionAmount = Money.FromDecimal(Math.Abs(amount));
            if (amount < 0m)
            {
                envelope.Spend(transactionAmount, occurredAt);
            }
            else
            {
                envelope.Allocate(transactionAmount, occurredAt);
            }
        }

        await transactionRepository.AddTransactionAsync(transaction, splitEntries, cancellationToken);
        return Map(transaction, splitEntries);
    }

    public async Task<IReadOnlyList<TransactionDetails>> ListAsync(
        Guid? accountId,
        CancellationToken cancellationToken = default)
    {
        var transactions = await transactionRepository.ListTransactionsAsync(accountId, cancellationToken);
        var transactionIds = transactions.Select(static transaction => transaction.Id).ToArray();
        var splits = await transactionRepository.ListTransactionSplitsAsync(transactionIds, cancellationToken);
        var splitsByTransaction = splits
            .GroupBy(static split => split.TransactionId)
            .ToDictionary(static group => group.Key, static group => group.ToArray());

        return transactions
            .Select(transaction =>
            {
                splitsByTransaction.TryGetValue(transaction.Id, out var transactionSplits);
                return Map(transaction, transactionSplits ?? []);
            })
            .ToArray();
    }

    public async Task<TransactionDetails> UpdateAsync(
        Guid transactionId,
        string description,
        string merchant,
        string? category,
        bool replaceAllocation,
        Guid? envelopeId,
        IReadOnlyList<TransactionSplitCreateDetails>? splits,
        CancellationToken cancellationToken = default)
    {
        var transaction = await transactionRepository.GetTransactionByIdForUpdateAsync(transactionId, cancellationToken);
        if (transaction is null)
        {
            throw new DomainValidationException("Transaction was not found.");
        }

        if (transaction.DeletedAtUtc.HasValue)
        {
            throw new DomainValidationException("Deleted transactions cannot be updated.");
        }

        if (replaceAllocation)
        {
            if (envelopeId.HasValue && splits is { Count: > 0 })
            {
                throw new DomainValidationException("EnvelopeId cannot be set when splits are provided.");
            }

            if (splits is { Count: > 0 })
            {
                var splitTotal = splits.Sum(static split => split.Amount);
                if (splitTotal != transaction.Amount.Amount)
                {
                    throw new DomainValidationException("Split totals must equal transaction amount.");
                }
            }

            var existingSplits = await transactionRepository.ListTransactionSplitsByTransactionIdAsync(transactionId, cancellationToken);
            await RebalanceEnvelopesForAllocationChangeAsync(
                transaction,
                existingSplits,
                envelopeId,
                splits,
                cancellationToken);

            var updatedSplitEntries = splits is { Count: > 0 }
                ? splits.Select(split => new TransactionSplitEntry(
                        Guid.NewGuid(),
                        transaction.Id,
                        split.EnvelopeId,
                        Money.FromDecimal(split.Amount),
                        split.Category,
                        split.Notes))
                    .ToArray()
                : [];
            await transactionRepository.ReplaceTransactionSplitsAsync(
                transaction.Id,
                updatedSplitEntries,
                cancellationToken);

            transaction.AssignEnvelope(envelopeId);
        }

        transaction.UpdateMetadata(description, merchant, category);
        await transactionRepository.SaveChangesAsync(cancellationToken);

        var refreshedSplits = await transactionRepository.ListTransactionSplitsAsync([transactionId], cancellationToken);
        return Map(transaction, refreshedSplits);
    }

    public async Task DeleteAsync(
        Guid transactionId,
        string? deletedByUserId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await transactionRepository.GetTransactionByIdForUpdateAsync(transactionId, cancellationToken);
        if (transaction is null)
        {
            throw new DomainValidationException("Transaction was not found.");
        }

        var existingSplits = await transactionRepository.ListTransactionSplitsByTransactionIdAsync(transactionId, cancellationToken);

        if (existingSplits.Count > 0)
        {
            foreach (var split in existingSplits)
            {
                var envelope = await envelopeRepository.GetByIdForUpdateAsync(split.EnvelopeId, cancellationToken);
                if (envelope is null)
                {
                    throw new DomainValidationException($"Envelope was not found for split id {split.EnvelopeId}.");
                }

                ApplyReverseAmountToEnvelope(envelope, split.Amount.Amount, transaction.OccurredAt);
            }
        }
        else if (transaction.EnvelopeId.HasValue)
        {
            var envelope = await envelopeRepository.GetByIdForUpdateAsync(transaction.EnvelopeId.Value, cancellationToken);
            if (envelope is null)
            {
                throw new DomainValidationException("Envelope was not found.");
            }

            ApplyReverseAmountToEnvelope(envelope, transaction.Amount.Amount, transaction.OccurredAt);
        }

        transaction.SoftDelete(DateTimeOffset.UtcNow, deletedByUserId);
        await transactionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<TransactionDetails> RestoreAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await transactionRepository.GetTransactionByIdForUpdateAsync(transactionId, cancellationToken);
        if (transaction is null)
        {
            throw new DomainValidationException("Transaction was not found.");
        }

        if (!transaction.DeletedAtUtc.HasValue)
        {
            throw new DomainValidationException("Transaction is not deleted.");
        }

        var existingSplits = await transactionRepository.ListTransactionSplitsByTransactionIdAsync(transactionId, cancellationToken);

        if (existingSplits.Count > 0)
        {
            foreach (var split in existingSplits)
            {
                var envelope = await envelopeRepository.GetByIdForUpdateAsync(split.EnvelopeId, cancellationToken);
                if (envelope is null)
                {
                    throw new DomainValidationException($"Envelope was not found for split id {split.EnvelopeId}.");
                }

                ApplyTransactionAmountToEnvelope(envelope, split.Amount.Amount, transaction.OccurredAt);
            }
        }
        else if (transaction.EnvelopeId.HasValue)
        {
            var envelope = await envelopeRepository.GetByIdForUpdateAsync(transaction.EnvelopeId.Value, cancellationToken);
            if (envelope is null)
            {
                throw new DomainValidationException("Envelope was not found.");
            }

            ApplyTransactionAmountToEnvelope(envelope, transaction.Amount.Amount, transaction.OccurredAt);
        }

        transaction.Restore();
        await transactionRepository.SaveChangesAsync(cancellationToken);

        var refreshedSplits = await transactionRepository.ListTransactionSplitsByTransactionIdAsync(transactionId, cancellationToken);
        return Map(transaction, refreshedSplits);
    }

    public async Task<IReadOnlyList<TransactionDetails>> ListDeletedAsync(
        Guid familyId,
        int days,
        CancellationToken cancellationToken = default)
    {
        var boundedDays = Math.Clamp(days, 1, 90);
        var deletedSinceUtc = DateTimeOffset.UtcNow.AddDays(-boundedDays);
        var transactions = await transactionRepository.ListDeletedTransactionsByFamilyAsync(
            familyId,
            deletedSinceUtc,
            cancellationToken);

        var transactionIds = transactions.Select(static transaction => transaction.Id).ToArray();
        var splits = await transactionRepository.ListTransactionSplitsAsync(transactionIds, cancellationToken);
        var splitsByTransaction = splits
            .GroupBy(static split => split.TransactionId)
            .ToDictionary(static group => group.Key, static group => group.ToArray());

        return transactions
            .Select(transaction =>
            {
                splitsByTransaction.TryGetValue(transaction.Id, out var transactionSplits);
                return Map(transaction, transactionSplits ?? []);
            })
            .ToArray();
    }

    private async Task RebalanceEnvelopesForAllocationChangeAsync(
        Transaction transaction,
        IReadOnlyList<TransactionSplitEntry> existingSplits,
        Guid? updatedEnvelopeId,
        IReadOnlyList<TransactionSplitCreateDetails>? updatedSplits,
        CancellationToken cancellationToken)
    {
        if (existingSplits.Count > 0)
        {
            foreach (var split in existingSplits)
            {
                var envelope = await envelopeRepository.GetByIdForUpdateAsync(split.EnvelopeId, cancellationToken);
                if (envelope is null)
                {
                    throw new DomainValidationException($"Envelope was not found for split id {split.EnvelopeId}.");
                }

                ApplyReverseAmountToEnvelope(envelope, split.Amount.Amount, transaction.OccurredAt);
            }
        }
        else if (transaction.EnvelopeId.HasValue)
        {
            var currentEnvelope = await envelopeRepository.GetByIdForUpdateAsync(transaction.EnvelopeId.Value, cancellationToken);
            if (currentEnvelope is null)
            {
                throw new DomainValidationException("Envelope was not found.");
            }

            ApplyReverseAmountToEnvelope(currentEnvelope, transaction.Amount.Amount, transaction.OccurredAt);
        }

        if (updatedSplits is { Count: > 0 })
        {
            foreach (var split in updatedSplits)
            {
                var envelope = await envelopeRepository.GetByIdForUpdateAsync(split.EnvelopeId, cancellationToken);
                if (envelope is null)
                {
                    throw new DomainValidationException($"Envelope was not found for split id {split.EnvelopeId}.");
                }

                ApplyTransactionAmountToEnvelope(envelope, split.Amount, transaction.OccurredAt);
            }

            return;
        }

        if (updatedEnvelopeId.HasValue)
        {
            var updatedEnvelope = await envelopeRepository.GetByIdForUpdateAsync(updatedEnvelopeId.Value, cancellationToken);
            if (updatedEnvelope is null)
            {
                throw new DomainValidationException("Envelope was not found.");
            }

            ApplyTransactionAmountToEnvelope(updatedEnvelope, transaction.Amount.Amount, transaction.OccurredAt);
        }
    }

    private static void ApplyTransactionAmountToEnvelope(Envelope envelope, decimal amount, DateTimeOffset occurredAt)
    {
        var absoluteAmount = Money.FromDecimal(Math.Abs(amount));
        if (amount < 0m)
        {
            envelope.Spend(absoluteAmount, occurredAt);
        }
        else
        {
            envelope.Allocate(absoluteAmount, occurredAt);
        }
    }

    private static void ApplyReverseAmountToEnvelope(Envelope envelope, decimal amount, DateTimeOffset occurredAt)
    {
        var absoluteAmount = Money.FromDecimal(Math.Abs(amount));
        if (amount < 0m)
        {
            envelope.Allocate(absoluteAmount, occurredAt);
        }
        else
        {
            envelope.Spend(absoluteAmount, occurredAt);
        }
    }

    private static TransactionDetails Map(
        Transaction transaction,
        IReadOnlyList<TransactionSplitEntry> splitEntries)
    {
        return new TransactionDetails(
            transaction.Id,
            transaction.AccountId,
            transaction.Amount.Amount,
            transaction.Description,
            transaction.Merchant,
            transaction.OccurredAt,
            transaction.Category,
            transaction.EnvelopeId,
            splitEntries.Select(static split => new TransactionSplitDetails(
                    split.Id,
                    split.TransactionId,
                    split.EnvelopeId,
                    split.Amount.Amount,
                    split.Category,
                    split.Notes))
                .ToArray(),
            transaction.DeletedAtUtc,
            transaction.DeletedByUserId);
    }
}
