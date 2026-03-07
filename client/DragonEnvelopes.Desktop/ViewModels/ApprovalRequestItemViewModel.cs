namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record ApprovalRequestItemViewModel(
    Guid Id,
    Guid FamilyId,
    Guid AccountId,
    string RequestedByUserId,
    string RequestedByRole,
    decimal AmountValue,
    string AmountDisplay,
    string Description,
    string Merchant,
    DateTimeOffset OccurredAt,
    string OccurredOnDisplay,
    string? Category,
    Guid? EnvelopeId,
    string Status,
    string StatusDisplay,
    string RequestNotes,
    string ResolutionNotes,
    string? ResolvedByRole,
    DateTimeOffset CreatedAtUtc,
    string CreatedAtDisplay,
    DateTimeOffset? ResolvedAtUtc,
    Guid? ApprovedTransactionId)
{
    public bool IsPending => string.Equals(Status, "Pending", StringComparison.OrdinalIgnoreCase);
}
