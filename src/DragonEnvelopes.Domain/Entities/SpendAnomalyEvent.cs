namespace DragonEnvelopes.Domain.Entities;

public sealed class SpendAnomalyEvent
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public Guid TransactionId { get; }
    public Guid AccountId { get; }
    public string Merchant { get; }
    public decimal Amount { get; }
    public decimal BaselineAverageAmount { get; }
    public decimal BaselineStandardDeviation { get; }
    public int BaselineSampleSize { get; }
    public decimal DeviationRatio { get; }
    public int SeverityScore { get; }
    public string Reason { get; }
    public DateTimeOffset DetectedAtUtc { get; }

    public SpendAnomalyEvent(
        Guid id,
        Guid familyId,
        Guid transactionId,
        Guid accountId,
        string merchant,
        decimal amount,
        decimal baselineAverageAmount,
        decimal baselineStandardDeviation,
        int baselineSampleSize,
        decimal deviationRatio,
        int severityScore,
        string reason,
        DateTimeOffset detectedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Spend anomaly event id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (transactionId == Guid.Empty)
        {
            throw new DomainValidationException("Transaction id is required.");
        }

        if (accountId == Guid.Empty)
        {
            throw new DomainValidationException("Account id is required.");
        }

        if (amount <= 0m)
        {
            throw new DomainValidationException("Amount must be greater than zero.");
        }

        if (baselineAverageAmount < 0m)
        {
            throw new DomainValidationException("Baseline average amount cannot be negative.");
        }

        if (baselineStandardDeviation < 0m)
        {
            throw new DomainValidationException("Baseline standard deviation cannot be negative.");
        }

        if (baselineSampleSize <= 0)
        {
            throw new DomainValidationException("Baseline sample size must be greater than zero.");
        }

        if (deviationRatio < 0m)
        {
            throw new DomainValidationException("Deviation ratio cannot be negative.");
        }

        if (severityScore is < 1 or > 100)
        {
            throw new DomainValidationException("Severity score must be between 1 and 100.");
        }

        if (string.IsNullOrWhiteSpace(merchant))
        {
            throw new DomainValidationException("Merchant is required.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException("Reason is required.");
        }

        Id = id;
        FamilyId = familyId;
        TransactionId = transactionId;
        AccountId = accountId;
        Merchant = merchant.Trim();
        Amount = amount;
        BaselineAverageAmount = baselineAverageAmount;
        BaselineStandardDeviation = baselineStandardDeviation;
        BaselineSampleSize = baselineSampleSize;
        DeviationRatio = deviationRatio;
        SeverityScore = severityScore;
        Reason = reason.Trim();
        DetectedAtUtc = detectedAtUtc;
    }
}
