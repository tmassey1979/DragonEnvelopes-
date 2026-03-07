namespace DragonEnvelopes.Application.Cqrs.Messaging;

public static class IntegrationEventSourceServices
{
    public const string FamilyApi = "family-api";
    public const string LedgerApi = "ledger-api";
    public const string PlanningApi = "planning-api";
    public const string AutomationApi = "automation-api";
    public const string FinancialApi = "financial-api";
}

public sealed record EnvelopeCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid EnvelopeId,
    string Name,
    decimal MonthlyBudget,
    decimal CurrentBalance,
    string RolloverMode,
    decimal? RolloverCap,
    bool IsArchived);

public sealed record EnvelopeUpdatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid EnvelopeId,
    string Name,
    decimal MonthlyBudget,
    decimal CurrentBalance,
    string RolloverMode,
    decimal? RolloverCap,
    bool IsArchived);

public sealed record EnvelopeArchivedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid EnvelopeId,
    string Name,
    decimal CurrentBalance);

public sealed record EnvelopeRolloverPolicyUpdatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid EnvelopeId,
    string RolloverMode,
    decimal? RolloverCap);

public sealed record BudgetCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid BudgetId,
    string Month,
    decimal TotalIncome,
    decimal AllocatedAmount,
    decimal RemainingAmount);

public sealed record BudgetUpdatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid BudgetId,
    string Month,
    decimal TotalIncome,
    decimal AllocatedAmount,
    decimal RemainingAmount);

public sealed record RecurringBillCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid RecurringBillId,
    string Name,
    string Merchant,
    decimal Amount,
    string Frequency,
    int DayOfMonth,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive);

public sealed record RecurringBillUpdatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid RecurringBillId,
    string Name,
    string Merchant,
    decimal Amount,
    string Frequency,
    int DayOfMonth,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive);

public sealed record RecurringBillDeletedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid RecurringBillId,
    string Name,
    string Merchant,
    decimal Amount,
    string Frequency);

public sealed record EnvelopeGoalCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid GoalId,
    Guid EnvelopeId,
    decimal TargetAmount,
    DateOnly DueDate,
    string Status);

public sealed record EnvelopeGoalUpdatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid GoalId,
    Guid EnvelopeId,
    decimal TargetAmount,
    DateOnly DueDate,
    string Status);

public sealed record EnvelopeGoalDeletedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid GoalId,
    Guid EnvelopeId);

public sealed record AutomationRuleCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid RuleId,
    string Name,
    string RuleType,
    int Priority,
    bool IsEnabled);

public sealed record AutomationRuleUpdatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid RuleId,
    string Name,
    string RuleType,
    int Priority,
    bool IsEnabled);

public sealed record AutomationRuleEnabledIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid RuleId,
    string Name,
    string RuleType,
    int Priority);

public sealed record AutomationRuleDisabledIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid RuleId,
    string Name,
    string RuleType,
    int Priority);

public sealed record AutomationRuleDeletedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid RuleId,
    string Name,
    string RuleType,
    int Priority,
    bool IsEnabled);

public sealed record AutomationRuleExecutedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid TransactionId,
    string ExecutionType,
    string? AssignedCategory,
    bool AppliedSplits,
    int SplitCount);

public sealed record StripeFinancialAccountProvisionedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid EnvelopeId,
    Guid EnvelopeFinancialAccountId,
    string Provider,
    string ProviderFinancialAccountId,
    bool IsRebind);

public sealed record CardVirtualIssuedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid EnvelopeId,
    Guid CardId,
    string Provider,
    string ProviderCardId,
    string Status,
    string Brand,
    string Last4);

public sealed record CardPhysicalIssuedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid EnvelopeId,
    Guid CardId,
    string Provider,
    string ProviderCardId,
    string Status,
    string? ShipmentStatus,
    string? ShipmentCarrier,
    string? ShipmentTrackingNumber);

public sealed record CardFrozenIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid EnvelopeId,
    Guid CardId,
    string Provider,
    string ProviderCardId,
    string Status);

public sealed record CardUnfrozenIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid EnvelopeId,
    Guid CardId,
    string Provider,
    string ProviderCardId,
    string Status);

public sealed record CardCancelledIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid EnvelopeId,
    Guid CardId,
    string Provider,
    string ProviderCardId,
    string Status);

public sealed record ProviderNotificationDispatchFailedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid NotificationEventId,
    string UserId,
    string Channel,
    decimal Amount,
    string Merchant,
    int AttemptCount,
    string ErrorMessage);

public sealed record ProviderNotificationDispatchRetriedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid NotificationEventId,
    string UserId,
    string Channel,
    decimal Amount,
    string Merchant,
    int AttemptCount,
    string Status);
