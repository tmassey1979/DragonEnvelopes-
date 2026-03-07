namespace DragonEnvelopes.Contracts.IntegrationEvents;

public sealed record IntegrationEventEnvelope<TPayload>(
    string EventId,
    string EventName,
    string SchemaVersion,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset PublishedAtUtc,
    string SourceService,
    string CorrelationId,
    string? CausationId,
    Guid? FamilyId,
    TPayload Payload);
