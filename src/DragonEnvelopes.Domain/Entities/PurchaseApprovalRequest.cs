namespace DragonEnvelopes.Domain.Entities;

public sealed class PurchaseApprovalRequest
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public Guid AccountId { get; }
    public string RequestedByUserId { get; }
    public string RequestedByRole { get; }
    public decimal Amount { get; }
    public string Description { get; }
    public string Merchant { get; }
    public DateTimeOffset OccurredAt { get; }
    public string? Category { get; }
    public Guid? EnvelopeId { get; }
    public PurchaseApprovalRequestStatus Status { get; private set; }
    public string? RequestNotes { get; }
    public string? ResolutionNotes { get; private set; }
    public string? ResolvedByUserId { get; private set; }
    public string? ResolvedByRole { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public Guid? ApprovedTransactionId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public PurchaseApprovalRequest(
        Guid id,
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
        PurchaseApprovalRequestStatus status,
        string? requestNotes,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Approval request id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (accountId == Guid.Empty)
        {
            throw new DomainValidationException("Account id is required.");
        }

        if (amount == 0m)
        {
            throw new DomainValidationException("Approval request amount cannot be zero.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainValidationException("Approval request status is invalid.");
        }

        Id = id;
        FamilyId = familyId;
        AccountId = accountId;
        RequestedByUserId = NormalizeRequired(requestedByUserId, "Requested by user id");
        RequestedByRole = NormalizeRequired(requestedByRole, "Requested by role");
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Description = NormalizeRequired(description, "Description");
        Merchant = NormalizeRequired(merchant, "Merchant");
        OccurredAt = occurredAt;
        Category = NormalizeOptional(category);
        EnvelopeId = envelopeId;
        Status = status;
        RequestNotes = NormalizeOptional(requestNotes);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public void Approve(
        string resolvedByUserId,
        string resolvedByRole,
        Guid approvedTransactionId,
        string? resolutionNotes,
        DateTimeOffset resolvedAtUtc)
    {
        EnsureAwaitingDecision();
        if (approvedTransactionId == Guid.Empty)
        {
            throw new DomainValidationException("Approved transaction id is required.");
        }

        Status = PurchaseApprovalRequestStatus.Approved;
        ApprovedTransactionId = approvedTransactionId;
        ResolvedByUserId = NormalizeRequired(resolvedByUserId, "Resolved by user id");
        ResolvedByRole = NormalizeRequired(resolvedByRole, "Resolved by role");
        ResolutionNotes = NormalizeOptional(resolutionNotes);
        ResolvedAtUtc = resolvedAtUtc;
        UpdatedAtUtc = resolvedAtUtc;
    }

    public void Deny(
        string resolvedByUserId,
        string resolvedByRole,
        string? resolutionNotes,
        DateTimeOffset resolvedAtUtc)
    {
        EnsureAwaitingDecision();

        Status = PurchaseApprovalRequestStatus.Denied;
        ApprovedTransactionId = null;
        ResolvedByUserId = NormalizeRequired(resolvedByUserId, "Resolved by user id");
        ResolvedByRole = NormalizeRequired(resolvedByRole, "Resolved by role");
        ResolutionNotes = NormalizeOptional(resolutionNotes);
        ResolvedAtUtc = resolvedAtUtc;
        UpdatedAtUtc = resolvedAtUtc;
    }

    private void EnsureAwaitingDecision()
    {
        if (Status is PurchaseApprovalRequestStatus.Approved or PurchaseApprovalRequestStatus.Denied)
        {
            throw new DomainValidationException("Approval request has already been resolved.");
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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
