using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Domain.Entities;

public sealed class EnvelopeRolloverRun
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public string Month { get; }
    public DateTimeOffset AppliedAtUtc { get; }
    public string? AppliedByUserId { get; }
    public int EnvelopeCount { get; }
    public decimal TotalRolloverBalance { get; }
    public string ResultJson { get; }

    public EnvelopeRolloverRun(
        Guid id,
        Guid familyId,
        string month,
        DateTimeOffset appliedAtUtc,
        string? appliedByUserId,
        int envelopeCount,
        decimal totalRolloverBalance,
        string resultJson)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Envelope rollover run id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (envelopeCount < 0)
        {
            throw new DomainValidationException("Envelope count cannot be negative.");
        }

        Id = id;
        FamilyId = familyId;
        Month = BudgetMonth.Parse(month).ToString();
        AppliedAtUtc = appliedAtUtc;
        AppliedByUserId = string.IsNullOrWhiteSpace(appliedByUserId)
            ? null
            : appliedByUserId.Trim();
        EnvelopeCount = envelopeCount;
        TotalRolloverBalance = Money.FromDecimal(totalRolloverBalance).EnsureNonNegative("Total rollover balance").Amount;
        ResultJson = NormalizeRequired(resultJson, "Rollover result payload");
    }

    private static string NormalizeRequired(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{field} is required.");
        }

        return value.Trim();
    }
}
