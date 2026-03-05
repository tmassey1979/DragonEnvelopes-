using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Domain.Entities;

public sealed class TransactionSplitEntry
{
    public Guid Id { get; }
    public Guid TransactionId { get; }
    public Guid EnvelopeId { get; }
    public Money Amount { get; }
    public string? Category { get; }
    public string? Notes { get; }

    public TransactionSplitEntry(
        Guid id,
        Guid transactionId,
        Guid envelopeId,
        Money amount,
        string? category,
        string? notes)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Split id is required.");
        }

        if (transactionId == Guid.Empty)
        {
            throw new DomainValidationException("Transaction id is required.");
        }

        if (envelopeId == Guid.Empty)
        {
            throw new DomainValidationException("Envelope id is required.");
        }

        if (amount.IsZero)
        {
            throw new DomainValidationException("Split amount cannot be zero.");
        }

        Id = id;
        TransactionId = transactionId;
        EnvelopeId = envelopeId;
        Amount = amount;
        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}
