namespace DragonEnvelopes.Domain.Entities;

public sealed class EnvelopePaymentCardControlAudit
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public Guid EnvelopeId { get; }
    public Guid CardId { get; }
    public string Action { get; }
    public string? PreviousStateJson { get; }
    public string NewStateJson { get; }
    public string ChangedBy { get; }
    public DateTimeOffset ChangedAtUtc { get; }

    public EnvelopePaymentCardControlAudit(
        Guid id,
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        string action,
        string? previousStateJson,
        string newStateJson,
        string changedBy,
        DateTimeOffset changedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Card control audit id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (envelopeId == Guid.Empty)
        {
            throw new DomainValidationException("Envelope id is required.");
        }

        if (cardId == Guid.Empty)
        {
            throw new DomainValidationException("Card id is required.");
        }

        Id = id;
        FamilyId = familyId;
        EnvelopeId = envelopeId;
        CardId = cardId;
        Action = NormalizeRequired(action, "Audit action");
        PreviousStateJson = NormalizeNullable(previousStateJson);
        NewStateJson = NormalizeRequired(newStateJson, "Audit new state");
        ChangedBy = NormalizeRequired(changedBy, "Changed by");
        ChangedAtUtc = changedAtUtc;
    }

    private static string NormalizeRequired(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{field} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
