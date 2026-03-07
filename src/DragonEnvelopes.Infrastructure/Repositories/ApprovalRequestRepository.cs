using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class ApprovalRequestRepository(DragonEnvelopesDbContext dbContext) : IApprovalRequestRepository
{
    public async Task AddAsync(
        PurchaseApprovalRequest approvalRequest,
        CancellationToken cancellationToken = default)
    {
        await dbContext.PurchaseApprovalRequests.AddAsync(approvalRequest, cancellationToken);
    }

    public Task<PurchaseApprovalRequest?> GetByIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PurchaseApprovalRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken);
    }

    public Task<PurchaseApprovalRequest?> GetByIdForUpdateAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PurchaseApprovalRequests
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken);
    }

    public Task<Guid?> GetRequestFamilyIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PurchaseApprovalRequests
            .AsNoTracking()
            .Where(x => x.Id == requestId)
            .Select(x => (Guid?)x.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseApprovalRequest>> ListByFamilyAsync(
        Guid familyId,
        PurchaseApprovalRequestStatus? status,
        int take,
        CancellationToken cancellationToken = default)
    {
        var normalizedTake = take <= 0 ? 50 : take;
        var query = dbContext.PurchaseApprovalRequests
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(normalizedTake)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddTimelineEventAsync(
        PurchaseApprovalTimelineEvent timelineEvent,
        CancellationToken cancellationToken = default)
    {
        await dbContext.PurchaseApprovalTimelineEvents.AddAsync(timelineEvent, cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseApprovalTimelineEvent>> ListTimelineByRequestAsync(
        Guid requestId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var normalizedTake = take <= 0 ? 50 : take;

        return await dbContext.PurchaseApprovalTimelineEvents
            .AsNoTracking()
            .Where(x => x.ApprovalRequestId == requestId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(normalizedTake)
            .ToArrayAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
