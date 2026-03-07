namespace DragonEnvelopes.Contracts.IntegrationEvents;

public static class IntegrationEventEnvelopeFactory
{
    public static IntegrationEventEnvelope<TPayload> Create<TPayload>(
        string eventName,
        string schemaVersion,
        string sourceService,
        string correlationId,
        string? causationId,
        Guid? familyId,
        TPayload payload,
        DateTimeOffset? occurredAtUtc = null,
        string? eventId = null)
    {
        return new IntegrationEventEnvelope<TPayload>(
            EventId: string.IsNullOrWhiteSpace(eventId) ? Guid.NewGuid().ToString("D") : eventId.Trim(),
            EventName: eventName.Trim(),
            SchemaVersion: schemaVersion.Trim(),
            OccurredAtUtc: occurredAtUtc ?? DateTimeOffset.UtcNow,
            PublishedAtUtc: DateTimeOffset.UtcNow,
            SourceService: sourceService.Trim(),
            CorrelationId: correlationId.Trim(),
            CausationId: string.IsNullOrWhiteSpace(causationId) ? null : causationId.Trim(),
            FamilyId: familyId,
            Payload: payload);
    }
}
