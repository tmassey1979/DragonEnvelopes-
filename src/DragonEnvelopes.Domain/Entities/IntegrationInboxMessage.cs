namespace DragonEnvelopes.Domain.Entities;

public sealed class IntegrationInboxMessage
{
    public Guid Id { get; }
    public string IdempotencyKey { get; }
    public string ConsumerName { get; }
    public string SourceService { get; }
    public string EventId { get; }
    public string EventName { get; }
    public string RoutingKey { get; }
    public string SchemaVersion { get; }
    public Guid? FamilyId { get; }
    public string PayloadJson { get; }
    public DateTimeOffset ReceivedAtUtc { get; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? LastAttemptAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public DateTimeOffset? DeadLetteredAtUtc { get; private set; }
    public string? LastError { get; private set; }

    public bool IsProcessed => ProcessedAtUtc.HasValue;
    public bool IsDeadLettered => DeadLetteredAtUtc.HasValue;

    public IntegrationInboxMessage(
        Guid id,
        string idempotencyKey,
        string consumerName,
        string sourceService,
        string eventId,
        string eventName,
        string routingKey,
        string schemaVersion,
        Guid? familyId,
        string payloadJson,
        DateTimeOffset receivedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Inbox message id is required.");
        }

        if (familyId.HasValue && familyId.Value == Guid.Empty)
        {
            throw new DomainValidationException("Inbox family id cannot be empty when provided.");
        }

        Id = id;
        IdempotencyKey = NormalizeRequired(idempotencyKey, "Inbox idempotency key");
        ConsumerName = NormalizeRequired(consumerName, "Inbox consumer name");
        SourceService = NormalizeRequired(sourceService, "Inbox source service");
        EventId = NormalizeRequired(eventId, "Inbox event id");
        EventName = NormalizeRequired(eventName, "Inbox event name");
        RoutingKey = NormalizeRequired(routingKey, "Inbox routing key");
        SchemaVersion = NormalizeRequired(schemaVersion, "Inbox schema version");
        FamilyId = familyId;
        PayloadJson = NormalizeRequired(payloadJson, "Inbox payload json");
        ReceivedAtUtc = receivedAtUtc;
        AttemptCount = 0;
    }

    public void RegisterAttempt(DateTimeOffset attemptedAtUtc)
    {
        if (IsProcessed)
        {
            throw new DomainValidationException("Processed inbox messages cannot register new attempts.");
        }

        AttemptCount += 1;
        LastAttemptAtUtc = attemptedAtUtc;
    }

    public void MarkProcessed(DateTimeOffset processedAtUtc)
    {
        if (IsDeadLettered)
        {
            throw new DomainValidationException("Dead-lettered inbox messages cannot be marked as processed.");
        }

        ProcessedAtUtc = processedAtUtc;
        LastError = null;
    }

    public void MarkRetry(string errorMessage, DateTimeOffset attemptedAtUtc)
    {
        if (IsProcessed)
        {
            throw new DomainValidationException("Processed inbox messages cannot be marked for retry.");
        }

        LastAttemptAtUtc = attemptedAtUtc;
        LastError = NormalizeRequired(errorMessage, "Inbox retry error");
    }

    public void MarkDeadLettered(string errorMessage, DateTimeOffset deadLetteredAtUtc)
    {
        if (IsProcessed)
        {
            throw new DomainValidationException("Processed inbox messages cannot be dead-lettered.");
        }

        DeadLetteredAtUtc = deadLetteredAtUtc;
        LastError = NormalizeRequired(errorMessage, "Inbox dead-letter error");
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
