namespace DragonEnvelopes.Contracts.Sagas;

public sealed record WorkflowSagaResponse(
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
