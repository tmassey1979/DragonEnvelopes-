using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IWorkflowSagaRepository
{
    Task<WorkflowSaga?> GetByIdAsync(Guid sagaId, CancellationToken cancellationToken = default);

    Task<WorkflowSaga?> GetByIdForUpdateAsync(Guid sagaId, CancellationToken cancellationToken = default);

    Task<WorkflowSaga?> GetByWorkflowAndCorrelationForUpdateAsync(
        string workflowType,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<WorkflowSaga?> GetLatestByFamilyAndWorkflowAsync(
        Guid familyId,
        string workflowType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowSaga>> ListByFamilyAsync(
        Guid familyId,
        string? workflowType,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowSagaTimelineEvent>> ListTimelineBySagaAsync(
        Guid sagaId,
        int take,
        CancellationToken cancellationToken = default);

    Task AddSagaAsync(WorkflowSaga saga, CancellationToken cancellationToken = default);

    Task AddTimelineEventAsync(WorkflowSagaTimelineEvent timelineEvent, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
