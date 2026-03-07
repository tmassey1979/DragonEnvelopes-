namespace DragonEnvelopes.Application.Cqrs.Messaging;

public static class IntegrationEventRoutingKeys
{
    public const string FamilyCreatedV1 = "family.family.created.v1";
    public const string FamilyMemberAddedV1 = "family.member.added.v1";
    public const string FamilyMemberRemovedV1 = "family.member.removed.v1";
    public const string FamilyInviteAcceptedV1 = "family.invite.accepted.v1";

    public const string LedgerTransactionCreatedV1 = "ledger.transaction.created.v1";
    public const string LedgerTransactionUpdatedV1 = "ledger.transaction.updated.v1";
    public const string LedgerTransactionDeletedV1 = "ledger.transaction.deleted.v1";
    public const string LedgerTransactionRestoredV1 = "ledger.transaction.restored.v1";
    public const string LedgerApprovalRequestCreatedV1 = "ledger.approval-request.created.v1";
    public const string LedgerApprovalRequestApprovedV1 = "ledger.approval-request.approved.v1";
    public const string LedgerApprovalRequestDeniedV1 = "ledger.approval-request.denied.v1";

    public const string PlanningEnvelopeCreatedV1 = "planning.envelope.created.v1";
    public const string PlanningEnvelopeUpdatedV1 = "planning.envelope.updated.v1";
    public const string PlanningEnvelopeArchivedV1 = "planning.envelope.archived.v1";
    public const string PlanningEnvelopeRolloverPolicyUpdatedV1 = "planning.envelope.rollover-policy-updated.v1";
    public const string PlanningBudgetCreatedV1 = "planning.budget.created.v1";
    public const string PlanningBudgetUpdatedV1 = "planning.budget.updated.v1";
    public const string PlanningRecurringBillCreatedV1 = "planning.recurring-bill.created.v1";
    public const string PlanningRecurringBillUpdatedV1 = "planning.recurring-bill.updated.v1";
    public const string PlanningRecurringBillDeletedV1 = "planning.recurring-bill.deleted.v1";
    public const string PlanningEnvelopeGoalCreatedV1 = "planning.envelope-goal.created.v1";
    public const string PlanningEnvelopeGoalUpdatedV1 = "planning.envelope-goal.updated.v1";
    public const string PlanningEnvelopeGoalDeletedV1 = "planning.envelope-goal.deleted.v1";

    public const string AutomationRuleCreatedV1 = "automation.rule.created.v1";
    public const string AutomationRuleUpdatedV1 = "automation.rule.updated.v1";
    public const string AutomationRuleEnabledV1 = "automation.rule.enabled.v1";
    public const string AutomationRuleDisabledV1 = "automation.rule.disabled.v1";
    public const string AutomationRuleDeletedV1 = "automation.rule.deleted.v1";
    public const string AutomationRuleExecutedV1 = "automation.rule.executed.v1";

    public const string FinancialStripeFinancialAccountProvisionedV1 = "financial.stripe.financial-account.provisioned.v1";
    public const string FinancialCardVirtualIssuedV1 = "financial.card.virtual-issued.v1";
    public const string FinancialCardPhysicalIssuedV1 = "financial.card.physical-issued.v1";
    public const string FinancialCardFrozenV1 = "financial.card.frozen.v1";
    public const string FinancialCardUnfrozenV1 = "financial.card.unfrozen.v1";
    public const string FinancialCardCancelledV1 = "financial.card.cancelled.v1";
    public const string FinancialProviderNotificationDispatchFailedV1 = "financial.provider-notification.dispatch-failed.v1";
    public const string FinancialProviderNotificationDispatchRetriedV1 = "financial.provider-notification.dispatch-retried.v1";
}

public sealed record LedgerTransactionCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    string Description,
    string Merchant,
    string? Category,
    Guid? EnvelopeId,
    bool IsSplit);
