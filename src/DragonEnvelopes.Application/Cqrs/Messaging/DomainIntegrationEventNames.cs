namespace DragonEnvelopes.Application.Cqrs.Messaging;

public static class PlanningIntegrationEventNames
{
    public const string EnvelopeCreated = "EnvelopeCreated";
    public const string EnvelopeUpdated = "EnvelopeUpdated";
    public const string EnvelopeArchived = "EnvelopeArchived";
    public const string EnvelopeRolloverPolicyUpdated = "EnvelopeRolloverPolicyUpdated";
    public const string BudgetCreated = "BudgetCreated";
    public const string BudgetUpdated = "BudgetUpdated";
    public const string RecurringBillCreated = "RecurringBillCreated";
    public const string RecurringBillUpdated = "RecurringBillUpdated";
    public const string RecurringBillDeleted = "RecurringBillDeleted";
    public const string EnvelopeGoalCreated = "EnvelopeGoalCreated";
    public const string EnvelopeGoalUpdated = "EnvelopeGoalUpdated";
    public const string EnvelopeGoalDeleted = "EnvelopeGoalDeleted";
}

public static class AutomationIntegrationEventNames
{
    public const string AutomationRuleCreated = "AutomationRuleCreated";
    public const string AutomationRuleUpdated = "AutomationRuleUpdated";
    public const string AutomationRuleEnabled = "AutomationRuleEnabled";
    public const string AutomationRuleDisabled = "AutomationRuleDisabled";
    public const string AutomationRuleDeleted = "AutomationRuleDeleted";
    public const string AutomationRuleExecuted = "AutomationRuleExecuted";
}

public static class FinancialIntegrationEventNames
{
    public const string StripeFinancialAccountProvisioned = "StripeFinancialAccountProvisioned";
    public const string CardVirtualIssued = "CardVirtualIssued";
    public const string CardPhysicalIssued = "CardPhysicalIssued";
    public const string CardFrozen = "CardFrozen";
    public const string CardUnfrozen = "CardUnfrozen";
    public const string CardCancelled = "CardCancelled";
    public const string ProviderNotificationDispatchFailed = "ProviderNotificationDispatchFailed";
    public const string ProviderNotificationDispatchRetried = "ProviderNotificationDispatchRetried";
}
