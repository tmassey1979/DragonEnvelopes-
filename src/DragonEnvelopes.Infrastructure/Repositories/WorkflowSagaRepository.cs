using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class WorkflowSagaRepository(DragonEnvelopesDbContext dbContext) : IWorkflowSagaRepository
{
    public Task<WorkflowSaga?> GetByIdAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        return dbContext.WorkflowSagas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sagaId, cancellationToken);
    }

    public Task<WorkflowSaga?> GetByIdForUpdateAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        return dbContext.WorkflowSagas
            .FirstOrDefaultAsync(x => x.Id == sagaId, cancellationToken);
    }

    public Task<WorkflowSaga?> GetByWorkflowAndCorrelationForUpdateAsync(
        string workflowType,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.WorkflowSagas
            .FirstOrDefaultAsync(
                x => x.WorkflowType == workflowType && x.CorrelationId == correlationId,
                cancellationToken);
    }

    public Task<WorkflowSaga?> GetLatestByFamilyAndWorkflowAsync(
        Guid familyId,
        string workflowType,
        CancellationToken cancellationToken = default)
    {
        return dbContext.WorkflowSagas
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId && x.WorkflowType == workflowType)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowSaga>> ListByFamilyAsync(
        Guid familyId,
        string? workflowType,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkflowSagas
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId);

        if (!string.IsNullOrWhiteSpace(workflowType))
        {
            query = query.Where(x => x.WorkflowType == workflowType);
        }

        return await query
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(Math.Clamp(take <= 0 ? 50 : take, 1, 200))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowSagaTimelineEvent>> ListTimelineBySagaAsync(
        Guid sagaId,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkflowSagaTimelineEvents
            .AsNoTracking()
            .Where(x => x.SagaId == sagaId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(Math.Clamp(take <= 0 ? 50 : take, 1, 500))
            .ToArrayAsync(cancellationToken);
    }

    public Task AddSagaAsync(WorkflowSaga saga, CancellationToken cancellationToken = default)
    {
        dbContext.WorkflowSagas.Add(saga);
        return Task.CompletedTask;
    }

    public Task AddTimelineEventAsync(WorkflowSagaTimelineEvent timelineEvent, CancellationToken cancellationToken = default)
    {
        dbContext.WorkflowSagaTimelineEvents.Add(timelineEvent);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
