namespace DragonEnvelopes.Domain.Entities;

public sealed class ReportEnvelopeBalanceProjection
{
    public Guid EnvelopeId { get; private set; }
    public Guid FamilyId { get; private set; }
    public string EnvelopeName { get; private set; }
    public decimal MonthlyBudget { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public bool IsArchived { get; private set; }
    public string LastEventId { get; private set; }
    public DateTimeOffset LastEventOccurredAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ReportEnvelopeBalanceProjection(
        Guid envelopeId,
        Guid familyId,
        string envelopeName,
        decimal monthlyBudget,
        decimal currentBalance,
        bool isArchived,
        string lastEventId,
        DateTimeOffset lastEventOccurredAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (envelopeId == Guid.Empty)
        {
            throw new DomainValidationException("Envelope id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (string.IsNullOrWhiteSpace(envelopeName))
        {
            throw new DomainValidationException("Envelope name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastEventId))
        {
            throw new DomainValidationException("Last event id is required.");
        }

        EnvelopeId = envelopeId;
        FamilyId = familyId;
        EnvelopeName = envelopeName.Trim();
        MonthlyBudget = monthlyBudget;
        CurrentBalance = currentBalance;
        IsArchived = isArchived;
        LastEventId = lastEventId.Trim();
        LastEventOccurredAtUtc = lastEventOccurredAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Apply(
        string envelopeName,
        decimal monthlyBudget,
        decimal currentBalance,
        bool isArchived,
        string lastEventId,
        DateTimeOffset lastEventOccurredAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(envelopeName))
        {
            throw new DomainValidationException("Envelope name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastEventId))
        {
            throw new DomainValidationException("Last event id is required.");
        }

        EnvelopeName = envelopeName.Trim();
        MonthlyBudget = monthlyBudget;
        CurrentBalance = currentBalance;
        IsArchived = isArchived;
        LastEventId = lastEventId.Trim();
        LastEventOccurredAtUtc = lastEventOccurredAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }
}
