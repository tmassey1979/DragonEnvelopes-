using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface ISagaOrchestrationService
{
    Task<WorkflowSagaDetails> StartOrGetAsync(
        string workflowType,
        Guid? familyId,
        string correlationId,
        string? referenceId,
        string initialStep,
        string? message,
        CancellationToken cancellationToken = default);

    Task<WorkflowSagaDetails> AssignFamilyAsync(
        Guid sagaId,
        Guid familyId,
        string step,
        string eventType,
        string? message,
        CancellationToken cancellationToken = default);

    Task<WorkflowSagaDetails> RecordAsync(
        Guid sagaId,
        string step,
        string eventType,
        string status,
        string? message,
        string? failureReason,
        string? compensationAction,
        bool markCompleted,
        CancellationToken cancellationToken = default);

    Task<WorkflowSagaDetails?> GetByIdAsync(
        Guid sagaId,
        CancellationToken cancellationToken = default);

    Task<WorkflowSagaDetails?> GetLatestByFamilyAndWorkflowAsync(
        Guid familyId,
        string workflowType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowSagaDetails>> ListByFamilyAsync(
        Guid familyId,
        string? workflowType,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowSagaTimelineEventDetails>> ListTimelineAsync(
        Guid sagaId,
        int take,
        CancellationToken cancellationToken = default);
}
