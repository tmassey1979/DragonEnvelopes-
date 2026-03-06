namespace DragonEnvelopes.Domain.Entities;

public sealed class SpendNotificationEvent
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public string UserId { get; }
    public Guid EnvelopeId { get; }
    public Guid CardId { get; }
    public string WebhookEventId { get; }
    public string Channel { get; }
    public decimal Amount { get; }
    public string Merchant { get; }
    public decimal RemainingBalance { get; }
    public string Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? LastAttemptAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? SentAtUtc { get; private set; }

    public SpendNotificationEvent(
        Guid id,
        Guid familyId,
        string userId,
        Guid envelopeId,
        Guid cardId,
        string webhookEventId,
        string channel,
        decimal amount,
        string merchant,
        decimal remainingBalance,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Notification event id is required.");
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

        if (amount <= 0m)
        {
            throw new DomainValidationException("Notification amount must be greater than zero.");
        }

        Id = id;
        FamilyId = familyId;
        UserId = NormalizeRequired(userId, "User id");
        EnvelopeId = envelopeId;
        CardId = cardId;
        WebhookEventId = NormalizeRequired(webhookEventId, "Webhook event id");
        Channel = NormalizeRequired(channel, "Notification channel");
        Amount = amount;
        Merchant = NormalizeRequired(merchant, "Merchant");
        RemainingBalance = remainingBalance;
        Status = "Queued";
        AttemptCount = 0;
        CreatedAtUtc = createdAtUtc;
    }

    public void MarkSent(DateTimeOffset sentAtUtc)
    {
        AttemptCount += 1;
        LastAttemptAtUtc = sentAtUtc;
        SentAtUtc = sentAtUtc;
        ErrorMessage = null;
        Status = "Sent";
    }

    public void MarkRetry(string errorMessage, DateTimeOffset attemptedAtUtc, int maxAttempts)
    {
        AttemptCount += 1;
        LastAttemptAtUtc = attemptedAtUtc;
        ErrorMessage = NormalizeRequired(errorMessage, "Error message");
        Status = AttemptCount >= maxAttempts ? "Failed" : "Queued";
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
