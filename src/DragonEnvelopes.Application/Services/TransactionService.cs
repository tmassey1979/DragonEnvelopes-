using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class TransactionService(
    ITransactionRepository transactionRepository,
    IEnvelopeRepository envelopeRepository) : ITransactionService
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
        if (!await transactionRepository.AccountExistsAsync(accountId, cancellationToken))
        {
            throw new DomainValidationException("Account was not found.");
        }

        var hasSplitItems = hasSplits && splits is { Count: > 0 };
        Envelope? envelope = null;
        if (envelopeId.HasValue && !hasSplitItems)
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
            var splitInputs = splits!;
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
                .ToArray());
    }
}
