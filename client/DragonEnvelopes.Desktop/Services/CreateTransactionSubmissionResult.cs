using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public sealed record CreateTransactionSubmissionResult(
    bool RequiresApproval,
    ApprovalRequestItemViewModel? ApprovalRequest);
