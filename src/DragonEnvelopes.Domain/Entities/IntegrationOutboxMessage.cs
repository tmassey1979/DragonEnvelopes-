namespace DragonEnvelopes.Domain.Entities;

public sealed class IntegrationOutboxMessage
{
    public Guid Id { get; }
    public Guid? FamilyId { get; }
    public string EventId { get; }
    public string RoutingKey { get; }
    public string EventName { get; }
    public string SchemaVersion { get; }
    public string SourceService { get; }
    public string CorrelationId { get; }
    public string? CausationId { get; }
    public string PayloadJson { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset NextAttemptAtUtc { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? DispatchedAtUtc { get; private set; }

    public IntegrationOutboxMessage(
        Guid id,
        Guid? familyId,
        string eventId,
        string routingKey,
        string eventName,
        string schemaVersion,
        string sourceService,
        string correlationId,
        string? causationId,
        string payloadJson,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Outbox message id is required.");
        }

        if (familyId.HasValue && familyId.Value == Guid.Empty)
        {
            throw new DomainValidationException("Outbox family id cannot be empty when provided.");
        }

        Id = id;
        FamilyId = familyId;
        EventId = NormalizeRequired(eventId, "Outbox event id");
        RoutingKey = NormalizeRequired(routingKey, "Outbox routing key");
        EventName = NormalizeRequired(eventName, "Outbox event name");
        SchemaVersion = NormalizeRequired(schemaVersion, "Outbox schema version");
        SourceService = NormalizeRequired(sourceService, "Outbox source service");
        CorrelationId = NormalizeRequired(correlationId, "Outbox correlation id");
        CausationId = NormalizeOptional(causationId);
        PayloadJson = NormalizeRequired(payloadJson, "Outbox payload json");
        OccurredAtUtc = occurredAtUtc;
        CreatedAtUtc = createdAtUtc;
        AttemptCount = 0;
        NextAttemptAtUtc = createdAtUtc;
        LastError = null;
        DispatchedAtUtc = null;
    }

    public void MarkDispatched(DateTimeOffset dispatchedAtUtc)
    {
        if (dispatchedAtUtc < CreatedAtUtc)
        {
            throw new DomainValidationException("Dispatch timestamp cannot be before creation.");
        }

        DispatchedAtUtc = dispatchedAtUtc;
        LastError = null;
    }

    public void MarkRetry(string errorMessage, DateTimeOffset attemptedAtUtc, TimeSpan retryDelay)
    {
        if (attemptedAtUtc < CreatedAtUtc)
        {
            throw new DomainValidationException("Retry timestamp cannot be before creation.");
        }

        var normalizedDelay = retryDelay <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(5)
            : retryDelay;

        AttemptCount += 1;
        LastError = NormalizeRequired(errorMessage, "Outbox retry error");
        NextAttemptAtUtc = attemptedAtUtc.Add(normalizedDelay);
    }

    private static string NormalizeRequired(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{field} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
