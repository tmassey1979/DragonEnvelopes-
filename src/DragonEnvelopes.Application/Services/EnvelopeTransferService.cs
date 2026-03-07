using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class EnvelopeTransferService(
    IEnvelopeRepository envelopeRepository,
    ITransactionRepository transactionRepository) : IEnvelopeTransferService
{
    public async Task<EnvelopeTransferDetails> CreateAsync(
        Guid familyId,
        Guid accountId,
        Guid fromEnvelopeId,
        Guid toEnvelopeId,
        decimal amount,
        DateTimeOffset occurredAt,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var transferAmount = Money.FromDecimal(amount);
        if (transferAmount <= Money.Zero)
        {
            throw new DomainValidationException("Transfer amount must be greater than zero.");
        }

        if (fromEnvelopeId == toEnvelopeId)
        {
            throw new DomainValidationException("From and to envelopes must be different.");
        }

        if (!await transactionRepository.AccountBelongsToFamilyAsync(accountId, familyId, cancellationToken))
        {
            throw new DomainValidationException("Account was not found for the specified family.");
        }

        var fromEnvelope = await envelopeRepository.GetByIdForUpdateAsync(fromEnvelopeId, cancellationToken);
        if (fromEnvelope is null || fromEnvelope.FamilyId != familyId)
        {
            throw new DomainValidationException("Source envelope was not found for the specified family.");
        }

        var toEnvelope = await envelopeRepository.GetByIdForUpdateAsync(toEnvelopeId, cancellationToken);
        if (toEnvelope is null || toEnvelope.FamilyId != familyId)
        {
            throw new DomainValidationException("Destination envelope was not found for the specified family.");
        }

        fromEnvelope.Spend(transferAmount, occurredAt);
        toEnvelope.Allocate(transferAmount, occurredAt);

        var transferId = Guid.NewGuid();
        var debitTransaction = new Transaction(
            Guid.NewGuid(),
            accountId,
            Money.FromDecimal(-transferAmount.Amount),
            BuildDescription(fromEnvelope.Name, toEnvelope.Name, notes),
            "Envelope Transfer",
            occurredAt,
            category: "Envelope Transfer",
            envelopeId: fromEnvelopeId,
            transferId: transferId,
            transferCounterpartyEnvelopeId: toEnvelopeId,
            transferDirection: "Debit");
        var creditTransaction = new Transaction(
            Guid.NewGuid(),
            accountId,
            transferAmount,
            BuildDescription(fromEnvelope.Name, toEnvelope.Name, notes),
            "Envelope Transfer",
            occurredAt,
            category: "Envelope Transfer",
            envelopeId: toEnvelopeId,
            transferId: transferId,
            transferCounterpartyEnvelopeId: fromEnvelopeId,
            transferDirection: "Credit");

        await transactionRepository.AddTransactionsAsync([debitTransaction, creditTransaction], cancellationToken);

        return new EnvelopeTransferDetails(
            transferId,
            familyId,
            accountId,
            fromEnvelopeId,
            toEnvelopeId,
            transferAmount.Amount,
            occurredAt,
            NormalizeOptional(notes),
            debitTransaction.Id,
            creditTransaction.Id);
    }

    private static string BuildDescription(string fromEnvelopeName, string toEnvelopeName, string? notes)
    {
        var summary = $"Envelope transfer {fromEnvelopeName} -> {toEnvelopeName}";
        var normalizedNotes = NormalizeOptional(notes);
        return string.IsNullOrWhiteSpace(normalizedNotes)
            ? summary
            : $"{summary} ({normalizedNotes})";
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
