namespace DragonEnvelopes.Application.DTOs;

public sealed record WorkflowSagaDetails(
    Guid Id,
    Guid? FamilyId,
    string WorkflowType,
    string CorrelationId,
    string? ReferenceId,
    string Status,
    string CurrentStep,
    string? FailureReason,
    string? CompensationAction,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record WorkflowSagaTimelineEventDetails(
    Guid Id,
    Guid SagaId,
    Guid? FamilyId,
    string WorkflowType,
    string Step,
    string EventType,
    string Status,
    string? Message,
    DateTimeOffset OccurredAtUtc);
