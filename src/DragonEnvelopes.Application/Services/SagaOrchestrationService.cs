using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Services;

public sealed class SagaOrchestrationService(
    IWorkflowSagaRepository workflowSagaRepository,
    IClock clock) : ISagaOrchestrationService
{
    public async Task<WorkflowSagaDetails> StartOrGetAsync(
        string workflowType,
        Guid? familyId,
        string correlationId,
        string? referenceId,
        string initialStep,
        string? message,
        CancellationToken cancellationToken = default)
    {
        var normalizedWorkflowType = NormalizeRequired(workflowType, "Workflow type");
        var normalizedCorrelationId = NormalizeRequired(correlationId, "Correlation id");
        var normalizedInitialStep = NormalizeRequired(initialStep, "Initial step");

        var existing = await workflowSagaRepository.GetByWorkflowAndCorrelationForUpdateAsync(
            normalizedWorkflowType,
            normalizedCorrelationId,
            cancellationToken);
        if (existing is not null)
        {
            if (familyId.HasValue)
            {
                existing.AssignFamily(familyId.Value, clock.UtcNow);
                await workflowSagaRepository.SaveChangesAsync(cancellationToken);
            }

            return Map(existing);
        }

        var now = clock.UtcNow;
        var saga = new WorkflowSaga(
            Guid.NewGuid(),
            familyId,
            normalizedWorkflowType,
            normalizedCorrelationId,
            NormalizeOptional(referenceId),
            WorkflowSagaStatuses.Running,
            normalizedInitialStep,
            failureReason: null,
            compensationAction: null,
            startedAtUtc: now,
            updatedAtUtc: now,
            completedAtUtc: null);
        await workflowSagaRepository.AddSagaAsync(saga, cancellationToken);
        await workflowSagaRepository.AddTimelineEventAsync(
            new WorkflowSagaTimelineEvent(
                Guid.NewGuid(),
                saga.Id,
                saga.FamilyId,
                saga.WorkflowType,
                normalizedInitialStep,
                eventType: "Started",
                saga.Status,
                NormalizeOptional(message),
                now),
            cancellationToken);
        await workflowSagaRepository.SaveChangesAsync(cancellationToken);
        return Map(saga);
    }

    public async Task<WorkflowSagaDetails> AssignFamilyAsync(
        Guid sagaId,
        Guid familyId,
        string step,
        string eventType,
        string? message,
        CancellationToken cancellationToken = default)
    {
        var saga = await workflowSagaRepository.GetByIdForUpdateAsync(sagaId, cancellationToken)
            ?? throw new DomainValidationException("Workflow saga was not found.");
        var now = clock.UtcNow;
        saga.AssignFamily(familyId, now);
        saga.Advance(
            NormalizeRequired(step, "Step"),
            saga.Status,
            saga.FailureReason,
            saga.CompensationAction,
            now,
            markCompleted: false);
        await workflowSagaRepository.AddTimelineEventAsync(
            new WorkflowSagaTimelineEvent(
                Guid.NewGuid(),
                saga.Id,
                saga.FamilyId,
                saga.WorkflowType,
                saga.CurrentStep,
                NormalizeRequired(eventType, "Event type"),
                saga.Status,
                NormalizeOptional(message),
                now),
            cancellationToken);
        await workflowSagaRepository.SaveChangesAsync(cancellationToken);
        return Map(saga);
    }

    public async Task<WorkflowSagaDetails> RecordAsync(
        Guid sagaId,
        string step,
        string eventType,
        string status,
        string? message,
        string? failureReason,
        string? compensationAction,
        bool markCompleted,
        CancellationToken cancellationToken = default)
    {
        var saga = await workflowSagaRepository.GetByIdForUpdateAsync(sagaId, cancellationToken)
            ?? throw new DomainValidationException("Workflow saga was not found.");
        var now = clock.UtcNow;

        var normalizedStep = NormalizeRequired(step, "Step");
        var normalizedStatus = NormalizeRequired(status, "Status");
        var normalizedEventType = NormalizeRequired(eventType, "Event type");
        var normalizedFailureReason = NormalizeOptional(failureReason);
        var normalizedCompensationAction = NormalizeOptional(compensationAction);

        saga.Advance(
            normalizedStep,
            normalizedStatus,
            normalizedFailureReason,
            normalizedCompensationAction,
            now,
            markCompleted);

        await workflowSagaRepository.AddTimelineEventAsync(
            new WorkflowSagaTimelineEvent(
                Guid.NewGuid(),
                saga.Id,
                saga.FamilyId,
                saga.WorkflowType,
                normalizedStep,
                normalizedEventType,
                normalizedStatus,
                NormalizeOptional(message),
                now),
            cancellationToken);
        await workflowSagaRepository.SaveChangesAsync(cancellationToken);
        return Map(saga);
    }

    public async Task<WorkflowSagaDetails?> GetByIdAsync(
        Guid sagaId,
        CancellationToken cancellationToken = default)
    {
        if (sagaId == Guid.Empty)
        {
            return null;
        }

        var saga = await workflowSagaRepository.GetByIdAsync(sagaId, cancellationToken);
        return saga is null ? null : Map(saga);
    }

    public async Task<WorkflowSagaDetails?> GetLatestByFamilyAndWorkflowAsync(
        Guid familyId,
        string workflowType,
        CancellationToken cancellationToken = default)
    {
        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        var saga = await workflowSagaRepository.GetLatestByFamilyAndWorkflowAsync(
            familyId,
            NormalizeRequired(workflowType, "Workflow type"),
            cancellationToken);
        return saga is null ? null : Map(saga);
    }

    public async Task<IReadOnlyList<WorkflowSagaDetails>> ListByFamilyAsync(
        Guid familyId,
        string? workflowType,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        var sagas = await workflowSagaRepository.ListByFamilyAsync(
            familyId,
            NormalizeOptional(workflowType),
            take,
            cancellationToken);
        return sagas.Select(Map).ToArray();
    }

    public async Task<IReadOnlyList<WorkflowSagaTimelineEventDetails>> ListTimelineAsync(
        Guid sagaId,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (sagaId == Guid.Empty)
        {
            throw new DomainValidationException("Workflow saga id is required.");
        }

        var timeline = await workflowSagaRepository.ListTimelineBySagaAsync(
            sagaId,
            take,
            cancellationToken);
        return timeline
            .OrderByDescending(static evt => evt.OccurredAtUtc)
            .Select(Map)
            .ToArray();
    }

    private static WorkflowSagaDetails Map(WorkflowSaga saga)
    {
        return new WorkflowSagaDetails(
            saga.Id,
            saga.FamilyId,
            saga.WorkflowType,
            saga.CorrelationId,
            saga.ReferenceId,
            saga.Status,
            saga.CurrentStep,
            saga.FailureReason,
            saga.CompensationAction,
            saga.StartedAtUtc,
            saga.UpdatedAtUtc,
            saga.CompletedAtUtc);
    }

    private static WorkflowSagaTimelineEventDetails Map(WorkflowSagaTimelineEvent timelineEvent)
    {
        return new WorkflowSagaTimelineEventDetails(
            timelineEvent.Id,
            timelineEvent.SagaId,
            timelineEvent.FamilyId,
            timelineEvent.WorkflowType,
            timelineEvent.Step,
            timelineEvent.EventType,
            timelineEvent.Status,
            timelineEvent.Message,
            timelineEvent.OccurredAtUtc);
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
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
