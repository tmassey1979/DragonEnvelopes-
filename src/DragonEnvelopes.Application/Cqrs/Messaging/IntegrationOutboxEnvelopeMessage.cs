namespace DragonEnvelopes.Application.Cqrs.Messaging;

public sealed record IntegrationOutboxEnvelopeMessage(
    string RoutingKey,
    string EventId,
    string EventName,
    string SchemaVersion,
    DateTimeOffset OccurredAtUtc,
    string SourceService,
    string CorrelationId,
    string? CausationId,
    Guid? FamilyId,
    string PayloadJson);
