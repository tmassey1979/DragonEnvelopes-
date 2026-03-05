namespace DragonEnvelopes.Domain.ValueObjects;

public readonly record struct TransactionSplit
{
    public Guid EnvelopeId { get; }
    public Money Amount { get; }
    public string? Category { get; }

    public TransactionSplit(Guid envelopeId, Money amount, string? category)
    {
        if (envelopeId == Guid.Empty)
        {
            throw new DomainValidationException("Split envelope id is required.");
        }

        if (amount.IsZero)
        {
            throw new DomainValidationException("Split amount cannot be zero.");
        }

        EnvelopeId = envelopeId;
        Amount = amount;
        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
    }
}
