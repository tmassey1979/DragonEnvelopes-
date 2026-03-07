using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Services;

public sealed class ApprovalWorkflowService(
    IApprovalPolicyRepository approvalPolicyRepository,
    IApprovalRequestRepository approvalRequestRepository,
    ITransactionRepository transactionRepository,
    ITransactionService transactionService,
    IClock clock) : IApprovalWorkflowService
{
    private static readonly string[] AllowedRoles = ["Parent", "Adult", "Teen", "Child"];

    public async Task<ApprovalPolicyDetails?> GetPolicyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        ValidateFamilyId(familyId);
        var policy = await approvalPolicyRepository.GetByFamilyIdAsync(familyId, cancellationToken);
        return policy is null ? null : Map(policy);
    }

    public async Task<ApprovalPolicyDetails> UpsertPolicyAsync(
        Guid familyId,
        bool isEnabled,
        decimal amountThreshold,
        IReadOnlyCollection<string> rolesRequiringApproval,
        CancellationToken cancellationToken = default)
    {
        ValidateFamilyId(familyId);
        if (!await approvalPolicyRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        var normalizedRoles = NormalizeRoles(rolesRequiringApproval);
        var now = clock.UtcNow;
        var existing = await approvalPolicyRepository.GetByFamilyIdForUpdateAsync(familyId, cancellationToken);
        if (existing is null)
        {
            var created = FamilyApprovalPolicy.Create(
                Guid.NewGuid(),
                familyId,
                isEnabled,
                amountThreshold,
                normalizedRoles,
                now);
            await approvalPolicyRepository.AddAsync(created, cancellationToken);
            await approvalPolicyRepository.SaveChangesAsync(cancellationToken);
            return Map(created);
        }

        existing.Update(
            isEnabled,
            amountThreshold,
            normalizedRoles,
            now);
        await approvalPolicyRepository.SaveChangesAsync(cancellationToken);
        return Map(existing);
    }

    public async Task<ApprovalRequestDetails?> TryCreateBlockedRequestAsync(
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
        CancellationToken cancellationToken = default)
    {
        ValidateFamilyId(familyId);
        await EnsureAccountBelongsToFamilyAsync(accountId, familyId, cancellationToken);

        var policy = await approvalPolicyRepository.GetByFamilyIdAsync(familyId, cancellationToken);
        if (policy is null || !policy.RequiresApproval(requestedByRole, amount))
        {
            return null;
        }

        var now = clock.UtcNow;
        var approvalRequest = new PurchaseApprovalRequest(
            Guid.NewGuid(),
            familyId,
            accountId,
            NormalizeRequired(requestedByUserId, "Requested by user id"),
            NormalizeRole(requestedByRole),
            amount,
            description,
            merchant,
            occurredAt,
            category,
            envelopeId,
            PurchaseApprovalRequestStatus.Blocked,
            requestNotes: $"Auto-blocked by approval policy. Threshold {policy.AmountThreshold:0.00}.",
            now);
        await approvalRequestRepository.AddAsync(approvalRequest, cancellationToken);
        await approvalRequestRepository.AddTimelineEventAsync(
            CreateTimelineEvent(
                approvalRequest,
                PurchaseApprovalTimelineEventType.Blocked,
                approvalRequest.RequestedByUserId,
                approvalRequest.RequestedByRole,
                approvalRequest.Status,
                approvalRequest.RequestNotes,
                now),
            cancellationToken);
        await approvalRequestRepository.SaveChangesAsync(cancellationToken);
        return Map(approvalRequest);
    }

    public async Task<ApprovalRequestDetails> CreateRequestAsync(
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
        CancellationToken cancellationToken = default)
    {
        ValidateFamilyId(familyId);
        await EnsureAccountBelongsToFamilyAsync(accountId, familyId, cancellationToken);

        var now = clock.UtcNow;
        var approvalRequest = new PurchaseApprovalRequest(
            Guid.NewGuid(),
            familyId,
            accountId,
            NormalizeRequired(requestedByUserId, "Requested by user id"),
            NormalizeRole(requestedByRole),
            amount,
            description,
            merchant,
            occurredAt,
            category,
            envelopeId,
            PurchaseApprovalRequestStatus.Pending,
            notes,
            now);
        await approvalRequestRepository.AddAsync(approvalRequest, cancellationToken);
        await approvalRequestRepository.AddTimelineEventAsync(
            CreateTimelineEvent(
                approvalRequest,
                PurchaseApprovalTimelineEventType.Created,
                approvalRequest.RequestedByUserId,
                approvalRequest.RequestedByRole,
                approvalRequest.Status,
                notes,
                now),
            cancellationToken);
        await approvalRequestRepository.SaveChangesAsync(cancellationToken);
        return Map(approvalRequest);
    }

    public async Task<IReadOnlyList<ApprovalRequestDetails>> ListRequestsAsync(
        Guid familyId,
        string? status,
        int take,
        CancellationToken cancellationToken = default)
    {
        ValidateFamilyId(familyId);
        var parsedStatus = ParseStatusOrNull(status);
        var boundedTake = Math.Clamp(take <= 0 ? 50 : take, 1, 500);
        var requests = await approvalRequestRepository.ListByFamilyAsync(
            familyId,
            parsedStatus,
            boundedTake,
            cancellationToken);
        return requests.Select(Map).ToArray();
    }

    public async Task<ApprovalRequestDetails> ApproveAsync(
        Guid requestId,
        string resolvedByUserId,
        string resolvedByRole,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var request = await approvalRequestRepository.GetByIdForUpdateAsync(requestId, cancellationToken)
            ?? throw new DomainValidationException("Approval request was not found.");

        var transaction = await transactionService.CreateAsync(
            request.AccountId,
            request.Amount,
            request.Description,
            request.Merchant,
            request.OccurredAt,
            request.Category,
            request.EnvelopeId,
            hasSplits: false,
            splits: null,
            cancellationToken);

        var normalizedResolverUserId = NormalizeRequired(resolvedByUserId, "Resolved by user id");
        var normalizedResolverRole = NormalizeRole(resolvedByRole);
        var now = clock.UtcNow;
        request.Approve(
            normalizedResolverUserId,
            normalizedResolverRole,
            transaction.Id,
            notes,
            now);

        await approvalRequestRepository.AddTimelineEventAsync(
            CreateTimelineEvent(
                request,
                PurchaseApprovalTimelineEventType.Approved,
                normalizedResolverUserId,
                normalizedResolverRole,
                request.Status,
                notes,
                now),
            cancellationToken);
        await approvalRequestRepository.SaveChangesAsync(cancellationToken);
        return Map(request);
    }

    public async Task<ApprovalRequestDetails> DenyAsync(
        Guid requestId,
        string resolvedByUserId,
        string resolvedByRole,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var request = await approvalRequestRepository.GetByIdForUpdateAsync(requestId, cancellationToken)
            ?? throw new DomainValidationException("Approval request was not found.");

        var normalizedResolverUserId = NormalizeRequired(resolvedByUserId, "Resolved by user id");
        var normalizedResolverRole = NormalizeRole(resolvedByRole);
        var now = clock.UtcNow;
        request.Deny(
            normalizedResolverUserId,
            normalizedResolverRole,
            notes,
            now);

        await approvalRequestRepository.AddTimelineEventAsync(
            CreateTimelineEvent(
                request,
                PurchaseApprovalTimelineEventType.Denied,
                normalizedResolverUserId,
                normalizedResolverRole,
                request.Status,
                notes,
                now),
            cancellationToken);
        await approvalRequestRepository.SaveChangesAsync(cancellationToken);
        return Map(request);
    }

    public async Task<IReadOnlyList<ApprovalTimelineEventDetails>> ListTimelineAsync(
        Guid requestId,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (requestId == Guid.Empty)
        {
            throw new DomainValidationException("Approval request id is required.");
        }

        var boundedTake = Math.Clamp(take <= 0 ? 50 : take, 1, 500);
        var timeline = await approvalRequestRepository.ListTimelineByRequestAsync(
            requestId,
            boundedTake,
            cancellationToken);
        return timeline
            .OrderByDescending(static evt => evt.OccurredAtUtc)
            .Select(Map)
            .ToArray();
    }

    private async Task EnsureAccountBelongsToFamilyAsync(
        Guid accountId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty)
        {
            throw new DomainValidationException("Account id is required.");
        }

        if (!await transactionRepository.AccountBelongsToFamilyAsync(accountId, familyId, cancellationToken))
        {
            throw new DomainValidationException("Account was not found for family.");
        }
    }

    private static IReadOnlyList<string> NormalizeRoles(IReadOnlyCollection<string> roles)
    {
        return roles
            .Select(static role => role?.Trim() ?? string.Empty)
            .Where(static role => !string.IsNullOrWhiteSpace(role))
            .Select(NormalizeRole)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new DomainValidationException("Role is required.");
        }

        var normalized = role.Trim();
        var matchedRole = AllowedRoles.FirstOrDefault(
            allowedRole => allowedRole.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (matchedRole is null)
        {
            throw new DomainValidationException("Role is invalid.");
        }

        return matchedRole;
    }

    private static PurchaseApprovalRequestStatus? ParseStatusOrNull(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        if (!Enum.TryParse<PurchaseApprovalRequestStatus>(
                status.Trim(),
                ignoreCase: true,
                out var parsed))
        {
            throw new DomainValidationException("Approval status is invalid.");
        }

        return parsed;
    }

    private static void ValidateFamilyId(Guid familyId)
    {
        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static ApprovalPolicyDetails Map(FamilyApprovalPolicy policy)
    {
        return new ApprovalPolicyDetails(
            policy.Id,
            policy.FamilyId,
            policy.IsEnabled,
            policy.AmountThreshold,
            policy.RolesRequiringApproval,
            policy.UpdatedAtUtc);
    }

    private static ApprovalRequestDetails Map(PurchaseApprovalRequest request)
    {
        return new ApprovalRequestDetails(
            request.Id,
            request.FamilyId,
            request.AccountId,
            request.RequestedByUserId,
            request.RequestedByRole,
            request.Amount,
            request.Description,
            request.Merchant,
            request.OccurredAt,
            request.Category,
            request.EnvelopeId,
            request.Status,
            request.RequestNotes,
            request.ResolutionNotes,
            request.ResolvedByUserId,
            request.ResolvedByRole,
            request.ResolvedAtUtc,
            request.ApprovedTransactionId,
            request.CreatedAtUtc,
            request.UpdatedAtUtc);
    }

    private static ApprovalTimelineEventDetails Map(PurchaseApprovalTimelineEvent timelineEvent)
    {
        return new ApprovalTimelineEventDetails(
            timelineEvent.Id,
            timelineEvent.FamilyId,
            timelineEvent.ApprovalRequestId,
            timelineEvent.EventType,
            timelineEvent.ActorUserId,
            timelineEvent.ActorRole,
            timelineEvent.Status,
            timelineEvent.Notes,
            timelineEvent.OccurredAtUtc);
    }

    private static PurchaseApprovalTimelineEvent CreateTimelineEvent(
        PurchaseApprovalRequest request,
        PurchaseApprovalTimelineEventType eventType,
        string actorUserId,
        string actorRole,
        PurchaseApprovalRequestStatus status,
        string? notes,
        DateTimeOffset occurredAtUtc)
    {
        return new PurchaseApprovalTimelineEvent(
            Guid.NewGuid(),
            request.FamilyId,
            request.Id,
            eventType,
            actorUserId,
            actorRole,
            status.ToString(),
            notes,
            occurredAtUtc);
    }
}
