namespace DragonEnvelopes.Contracts.Approvals;

public sealed record UpsertApprovalPolicyRequest(
    Guid FamilyId,
    bool IsEnabled,
    decimal AmountThreshold,
    IReadOnlyList<string> RolesRequiringApproval);

public sealed record ApprovalPolicyResponse(
    Guid Id,
    Guid FamilyId,
    bool IsEnabled,
    decimal AmountThreshold,
    IReadOnlyList<string> RolesRequiringApproval,
    DateTimeOffset UpdatedAtUtc);
