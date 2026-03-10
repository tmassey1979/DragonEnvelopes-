namespace DragonEnvelopes.Contracts.Sagas;

public sealed record WorkflowSagaTimelineEventResponse(
    Guid Id,
    Guid SagaId,
    Guid? FamilyId,
    string WorkflowType,
    string Step,
    string EventType,
    string Status,
    string? Message,
    DateTimeOffset OccurredAtUtc);
