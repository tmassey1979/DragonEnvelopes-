namespace DragonEnvelopes.Domain.Entities;

public sealed class PlaidWebhookEvent
{
    public Guid Id { get; }
    public string WebhookType { get; }
    public string? WebhookCode { get; }
    public string? ItemId { get; }
    public Guid? FamilyId { get; }
    public string ProcessingStatus { get; }
    public string? ErrorMessage { get; }
    public string PayloadJson { get; }
    public DateTimeOffset ReceivedAtUtc { get; }
    public DateTimeOffset ProcessedAtUtc { get; }

    public PlaidWebhookEvent(
        Guid id,
        string webhookType,
        string? webhookCode,
        string? itemId,
        Guid? familyId,
        string processingStatus,
        string? errorMessage,
        string payloadJson,
        DateTimeOffset receivedAtUtc,
        DateTimeOffset processedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Plaid webhook event id is required.");
        }

        if (processedAtUtc < receivedAtUtc)
        {
            throw new DomainValidationException("Processed time cannot be before received time.");
        }

        Id = id;
        WebhookType = NormalizeRequired(webhookType, "Webhook type");
        WebhookCode = NormalizeNullable(webhookCode);
        ItemId = NormalizeNullable(itemId);
        FamilyId = familyId;
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
