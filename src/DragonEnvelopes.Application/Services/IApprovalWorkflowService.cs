using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IApprovalWorkflowService
{
    Task<ApprovalPolicyDetails?> GetPolicyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<ApprovalPolicyDetails> UpsertPolicyAsync(
        Guid familyId,
        bool isEnabled,
        decimal amountThreshold,
        IReadOnlyCollection<string> rolesRequiringApproval,
        CancellationToken cancellationToken = default);

    Task<ApprovalRequestDetails?> TryCreateBlockedRequestAsync(
        Guid familyId,
        Guid accountId,
        string requestedByUserId,
        string requestedByRole,
        decimal amount,
        string description,
        string merchant,
        DateTimeOffset occurredAt,
        string? category,
        Guid? envelopeId,
        CancellationToken cancellationToken = default);

    Task<ApprovalRequestDetails> CreateRequestAsync(
        Guid familyId,
        Guid accountId,
        string requestedByUserId,
        string requestedByRole,
        decimal amount,
        string description,
        string merchant,
        DateTimeOffset occurredAt,
        string? category,
        Guid? envelopeId,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApprovalRequestDetails>> ListRequestsAsync(
        Guid familyId,
        string? status,
        int take,
        CancellationToken cancellationToken = default);

    Task<ApprovalRequestDetails> ApproveAsync(
        Guid requestId,
        string resolvedByUserId,
        string resolvedByRole,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<ApprovalRequestDetails> DenyAsync(
        Guid requestId,
        string resolvedByUserId,
        string resolvedByRole,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApprovalTimelineEventDetails>> ListTimelineAsync(
        Guid requestId,
        int take,
        CancellationToken cancellationToken = default);
}
