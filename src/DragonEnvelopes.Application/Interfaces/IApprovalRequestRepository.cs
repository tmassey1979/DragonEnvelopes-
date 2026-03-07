using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IApprovalRequestRepository
{
    Task AddAsync(
        PurchaseApprovalRequest approvalRequest,
        CancellationToken cancellationToken = default);

    Task<PurchaseApprovalRequest?> GetByIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<PurchaseApprovalRequest?> GetByIdForUpdateAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetRequestFamilyIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseApprovalRequest>> ListByFamilyAsync(
        Guid familyId,
        PurchaseApprovalRequestStatus? status,
        int take,
        CancellationToken cancellationToken = default);

    Task AddTimelineEventAsync(
        PurchaseApprovalTimelineEvent timelineEvent,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseApprovalTimelineEvent>> ListTimelineByRequestAsync(
        Guid requestId,
        int take,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
