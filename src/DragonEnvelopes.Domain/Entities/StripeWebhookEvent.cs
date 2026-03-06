namespace DragonEnvelopes.Domain.Entities;

public sealed class StripeWebhookEvent
{
    public Guid Id { get; }
    public string EventId { get; }
    public string EventType { get; }
    public Guid? FamilyId { get; }
    public Guid? EnvelopeId { get; }
    public Guid? CardId { get; }
    public string ProcessingStatus { get; }
    public string? ErrorMessage { get; }
    public string PayloadJson { get; }
    public DateTimeOffset ReceivedAtUtc { get; }
    public DateTimeOffset ProcessedAtUtc { get; }

    public StripeWebhookEvent(
        Guid id,
        string eventId,
        string eventType,
        Guid? familyId,
        Guid? envelopeId,
        Guid? cardId,
        string processingStatus,
        string? errorMessage,
        string payloadJson,
        DateTimeOffset receivedAtUtc,
        DateTimeOffset processedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Webhook event id is required.");
        }

        if (processedAtUtc < receivedAtUtc)
        {
            throw new DomainValidationException("Processed time cannot be before received time.");
        }

        Id = id;
        EventId = NormalizeRequired(eventId, "Event id");
        EventType = NormalizeRequired(eventType, "Event type");
        FamilyId = familyId;
        EnvelopeId = envelopeId;
        CardId = cardId;
        ProcessingStatus = NormalizeRequired(processingStatus, "Processing status");
        ErrorMessage = NormalizeNullable(errorMessage);
        PayloadJson = NormalizeRequired(payloadJson, "Payload json");
        ReceivedAtUtc = receivedAtUtc;
        ProcessedAtUtc = processedAtUtc;
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
