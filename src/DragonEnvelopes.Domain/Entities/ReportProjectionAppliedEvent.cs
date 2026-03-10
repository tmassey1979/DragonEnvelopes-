namespace DragonEnvelopes.Domain.Entities;

public sealed class ReportProjectionAppliedEvent
{
    public Guid OutboxMessageId { get; private set; }
    public string EventId { get; private set; }
    public Guid? FamilyId { get; private set; }
    public string RoutingKey { get; private set; }
    public string SourceService { get; private set; }
    public DateTimeOffset EventOccurredAtUtc { get; private set; }
    public DateTimeOffset AppliedAtUtc { get; private set; }
    public string ProcessingStatus { get; private set; }
    public string? ErrorMessage { get; private set; }

    public ReportProjectionAppliedEvent(
        Guid outboxMessageId,
        string eventId,
        Guid? familyId,
        string routingKey,
        string sourceService,
        DateTimeOffset eventOccurredAtUtc,
        DateTimeOffset appliedAtUtc,
        string processingStatus,
        string? errorMessage)
    {
        if (outboxMessageId == Guid.Empty)
        {
            throw new DomainValidationException("Outbox message id is required.");
        }

        if (familyId.HasValue && familyId.Value == Guid.Empty)
        {
            throw new DomainValidationException("Family id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(eventId))
        {
            throw new DomainValidationException("Event id is required.");
        }

        if (string.IsNullOrWhiteSpace(routingKey))
        {
            throw new DomainValidationException("Routing key is required.");
        }

        if (string.IsNullOrWhiteSpace(sourceService))
        {
            throw new DomainValidationException("Source service is required.");
        }

        if (string.IsNullOrWhiteSpace(processingStatus))
        {
            throw new DomainValidationException("Processing status is required.");
        }

        OutboxMessageId = outboxMessageId;
        EventId = eventId.Trim();
        FamilyId = familyId;
        RoutingKey = routingKey.Trim();
        SourceService = sourceService.Trim();
        EventOccurredAtUtc = eventOccurredAtUtc;
        AppliedAtUtc = appliedAtUtc;
        ProcessingStatus = processingStatus.Trim();
        ErrorMessage = NormalizeError(errorMessage);
    }

    private static string? NormalizeError(string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return null;
        }

        var normalized = errorMessage.Trim();
        return normalized.Length <= 1000
            ? normalized
            : normalized[..1000];
    }
}
