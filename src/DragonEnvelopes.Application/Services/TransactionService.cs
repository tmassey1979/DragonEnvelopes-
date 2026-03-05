using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class TransactionService(ITransactionRepository transactionRepository) : ITransactionService
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
        CancellationToken cancellationToken = default)
    {
        if (!await transactionRepository.AccountExistsAsync(accountId, cancellationToken))
        {
            throw new DomainValidationException("Account was not found.");
        }

        if (envelopeId.HasValue
            && !await transactionRepository.EnvelopeExistsAsync(envelopeId.Value, cancellationToken))
        {
            throw new DomainValidationException("Envelope was not found.");
        }

        if (hasSplits)
        {
            throw new DomainValidationException("Split transactions are not yet supported by persistence.");
        }

        var transaction = new Transaction(
            Guid.NewGuid(),
            accountId,
            Money.FromDecimal(amount),
            description,
            merchant,
            occurredAt,
            category,
            envelopeId);

        await transactionRepository.AddTransactionAsync(transaction, cancellationToken);
        return Map(transaction);
    }

    public async Task<IReadOnlyList<TransactionDetails>> ListAsync(
        Guid? accountId,
        CancellationToken cancellationToken = default)
    {
        var transactions = await transactionRepository.ListTransactionsAsync(accountId, cancellationToken);
        return transactions.Select(Map).ToArray();
    }

    private static TransactionDetails Map(Transaction transaction)
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
            []);
    }
}
