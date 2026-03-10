using System.Diagnostics;
using System.Text.Json;
using DragonEnvelopes.Application.Cqrs.Messaging;
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
    IClock clock,
    IIntegrationOutboxRepository? integrationOutboxRepository = null,
    ISagaOrchestrationService? sagaOrchestrationService = null) : IApprovalWorkflowService
{
    private static readonly string[] AllowedRoles = ["Parent", "Adult", "Teen", "Child"];
    private const string OutboxSchemaVersion = "1.0";
    private const string LedgerSourceService = "ledger-api";

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
        await EnqueueOutboxAsync(
            approvalRequest.FamilyId,
            IntegrationEventRoutingKeys.LedgerApprovalRequestCreatedV1,
            LedgerIntegrationEventNames.ApprovalRequestCreated,
            new ApprovalRequestCreatedIntegrationEvent(
                Guid.NewGuid(),
                now,
                approvalRequest.FamilyId,
                ResolveCorrelationId(),
                approvalRequest.Id,
                approvalRequest.AccountId,
                approvalRequest.Status.ToString(),
                approvalRequest.Amount,
                approvalRequest.RequestedByUserId,
                approvalRequest.RequestedByRole),
            cancellationToken);
        await approvalRequestRepository.SaveChangesAsync(cancellationToken);
        await TryRecordApprovalSagaAsync(
            approvalRequest,
            step: "ApprovalRequestBlocked",
            eventType: "StepCompleted",
            status: WorkflowSagaStatuses.Running,
            message: "Approval request auto-blocked by policy.",
            failureReason: null,
            compensationAction: null,
            markCompleted: false,
            cancellationToken);
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
        await EnqueueOutboxAsync(
            approvalRequest.FamilyId,
            IntegrationEventRoutingKeys.LedgerApprovalRequestCreatedV1,
            LedgerIntegrationEventNames.ApprovalRequestCreated,
            new ApprovalRequestCreatedIntegrationEvent(
                Guid.NewGuid(),
                now,
                approvalRequest.FamilyId,
                ResolveCorrelationId(),
                approvalRequest.Id,
                approvalRequest.AccountId,
                approvalRequest.Status.ToString(),
                approvalRequest.Amount,
                approvalRequest.RequestedByUserId,
                approvalRequest.RequestedByRole),
            cancellationToken);
        await approvalRequestRepository.SaveChangesAsync(cancellationToken);
        await TryRecordApprovalSagaAsync(
            approvalRequest,
            step: "ApprovalRequestCreated",
            eventType: "StepCompleted",
            status: WorkflowSagaStatuses.Running,
            message: "Approval request created and awaiting decision.",
            failureReason: null,
            compensationAction: null,
            markCompleted: false,
            cancellationToken);
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

        await TryRecordApprovalSagaAsync(
            request,
            step: "ApprovalResolutionStarted",
            eventType: "StepStarted",
            status: WorkflowSagaStatuses.Running,
            message: "Attempting to post approved transaction.",
            failureReason: null,
            compensationAction: null,
            markCompleted: false,
            cancellationToken);

        TransactionDetails transaction;
        try
        {
            transaction = await transactionService.CreateAsync(
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
        }
        catch (Exception ex)
        {
            await TryRecordApprovalSagaAsync(
                request,
                step: "ApprovalResolutionFailed",
                eventType: "StepFailed",
                status: WorkflowSagaStatuses.Failed,
                message: ex.Message,
                failureReason: ex.Message,
                compensationAction: "RequestRemainsPendingForManualRetry",
                markCompleted: false,
                cancellationToken);
            throw;
        }

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
        await EnqueueOutboxAsync(
            request.FamilyId,
            IntegrationEventRoutingKeys.LedgerApprovalRequestApprovedV1,
            LedgerIntegrationEventNames.ApprovalRequestApproved,
            new ApprovalRequestApprovedIntegrationEvent(
                Guid.NewGuid(),
                now,
                request.FamilyId,
                ResolveCorrelationId(),
                request.Id,
                request.AccountId,
                transaction.Id,
                normalizedResolverUserId,
                normalizedResolverRole),
            cancellationToken);
        await approvalRequestRepository.SaveChangesAsync(cancellationToken);
        await TryRecordApprovalSagaAsync(
            request,
            step: "ApprovalResolvedAndPosted",
            eventType: "StepCompleted",
            status: WorkflowSagaStatuses.Completed,
            message: $"Approval resolved and transaction '{transaction.Id}' posted.",
            failureReason: null,
            compensationAction: null,
            markCompleted: true,
            cancellationToken);
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
        await EnqueueOutboxAsync(
            request.FamilyId,
            IntegrationEventRoutingKeys.LedgerApprovalRequestDeniedV1,
            LedgerIntegrationEventNames.ApprovalRequestDenied,
            new ApprovalRequestDeniedIntegrationEvent(
                Guid.NewGuid(),
                now,
                request.FamilyId,
                ResolveCorrelationId(),
                request.Id,
                request.AccountId,
                normalizedResolverUserId,
                normalizedResolverRole),
            cancellationToken);
        await approvalRequestRepository.SaveChangesAsync(cancellationToken);
        await TryRecordApprovalSagaAsync(
            request,
            step: "ApprovalDenied",
            eventType: "Compensation",
            status: WorkflowSagaStatuses.Compensated,
            message: "Approval request denied. No ledger mutation posted.",
            failureReason: null,
            compensationAction: "NoTransactionPosted",
            markCompleted: true,
            cancellationToken);
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

    private async Task EnqueueOutboxAsync<TPayload>(
        Guid familyId,
        string routingKey,
        string eventName,
        TPayload payload,
        CancellationToken cancellationToken)
    {
        if (integrationOutboxRepository is null)
        {
            return;
        }

        var outboxMessage = new IntegrationOutboxMessage(
            Guid.NewGuid(),
            familyId,
            ResolveEventId(payload),
            routingKey,
            eventName,
            OutboxSchemaVersion,
            LedgerSourceService,
            ResolveCorrelationId(payload),
            causationId: null,
            JsonSerializer.Serialize(payload),
            ResolveOccurredAtUtc(payload),
            clock.UtcNow);
        await integrationOutboxRepository.AddAsync(outboxMessage, cancellationToken);
    }

    private static string ResolveCorrelationId<TPayload>(TPayload payload)
    {
        if (TryGetStringProperty(payload, "CorrelationId", out var correlationId))
        {
            return correlationId;
        }

        return ResolveCorrelationId();
    }

    private static string ResolveCorrelationId()
    {
        return Activity.Current?.Id ?? Guid.NewGuid().ToString("D");
    }

    private async Task TryRecordApprovalSagaAsync(
        PurchaseApprovalRequest request,
        string step,
        string eventType,
        string status,
        string? message,
        string? failureReason,
        string? compensationAction,
        bool markCompleted,
        CancellationToken cancellationToken)
    {
        if (sagaOrchestrationService is null)
        {
            return;
        }

        try
        {
            var saga = await sagaOrchestrationService.StartOrGetAsync(
                WorkflowSagaTypes.Approval,
                request.FamilyId,
                request.Id.ToString("D"),
                request.Id.ToString("D"),
                initialStep: "ApprovalWorkflowInitialized",
                message: "Approval workflow saga initialized.",
                cancellationToken);
            await sagaOrchestrationService.RecordAsync(
                saga.Id,
                step,
                eventType,
                status,
                message,
                failureReason,
                compensationAction,
                markCompleted,
                cancellationToken);
        }
        catch
        {
            // Best-effort saga tracking to avoid blocking approval request processing.
        }
    }

    private static string ResolveEventId<TPayload>(TPayload payload)
    {
        if (TryGetGuidProperty(payload, "EventId", out var eventId))
        {
            return eventId.ToString("D");
        }

        return Guid.NewGuid().ToString("D");
    }

    private static DateTimeOffset ResolveOccurredAtUtc<TPayload>(TPayload payload)
    {
        if (TryGetDateTimeOffsetProperty(payload, "OccurredAtUtc", out var occurredAtUtc))
        {
            return occurredAtUtc;
        }

        return DateTimeOffset.UtcNow;
    }

    private static bool TryGetStringProperty<TPayload>(TPayload payload, string propertyName, out string value)
    {
        value = string.Empty;
        var property = payload?.GetType().GetProperty(propertyName);
        if (property?.GetValue(payload) is not string stringValue || string.IsNullOrWhiteSpace(stringValue))
        {
            return false;
        }

        value = stringValue.Trim();
        return true;
    }

    private static bool TryGetGuidProperty<TPayload>(TPayload payload, string propertyName, out Guid value)
    {
        value = Guid.Empty;
        var property = payload?.GetType().GetProperty(propertyName);
        if (property?.GetValue(payload) is not Guid guidValue || guidValue == Guid.Empty)
        {
            return false;
        }

        value = guidValue;
        return true;
    }

    private static bool TryGetDateTimeOffsetProperty<TPayload>(TPayload payload, string propertyName, out DateTimeOffset value)
    {
        value = default;
        var property = payload?.GetType().GetProperty(propertyName);
        if (property?.GetValue(payload) is not DateTimeOffset dateTimeOffsetValue || dateTimeOffsetValue == default)
        {
            return false;
        }

        value = dateTimeOffsetValue;
        return true;
    }
}
