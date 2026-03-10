# Event Catalog v1

- Status: Active
- Date: 2026-03-07
- Owners: Platform + Backend
- Related Issues: #278, #289, #290, #291, #292, #293, #294, #300

## Purpose
This catalog defines canonical domain events, envelope metadata, and compatibility rules for DragonEnvelopes event-driven integration.

Machine-readable contract source used by CI compatibility gates:
- `docs/architecture/event-contract-catalog-v1.json`

## Standard Event Envelope
Every published event must include:

| Field | Type | Required | Notes |
|---|---|---|---|
| `eventId` | string (UUID) | yes | Global unique event identifier |
| `eventName` | string | yes | Stable canonical name from this catalog |
| `schemaVersion` | string | yes | Semantic event schema version, e.g. `1.0` |
| `occurredAtUtc` | string (ISO-8601) | yes | Source event time |
| `publishedAtUtc` | string (ISO-8601) | yes | Broker publish time |
| `sourceService` | string | yes | Producing service id |
| `correlationId` | string | yes | Request/workflow trace correlation |
| `causationId` | string | no | Upstream triggering event id/command id |
| `familyId` | string (UUID) | conditional | Required for family-scoped events |
| `payload` | object | yes | Event-specific body |

## Canonical Event Names (v1)
### Family Domain (`family-api`)
- `FamilyCreated`
- `FamilyProfileUpdated`
- `FamilyBudgetPreferencesUpdated`
- `FamilyMemberAdded`
- `FamilyMemberRoleUpdated`
- `FamilyMemberRemoved`
- `FamilyInviteCreated`
- `FamilyInviteResent`
- `FamilyInviteCancelled`
- `FamilyInviteRedeemed`
- `OnboardingProfileUpdated`
- `OnboardingReconciled`

### Ledger Domain (`ledger-api`)
- `AccountCreated`
- `AccountUpdated`
- `TransactionCreated`
- `TransactionUpdated`
- `TransactionDeleted`
- `TransactionRestored`
- `TransactionSplitApplied`
- `EnvelopeTransferPosted`
- `ApprovalPolicyUpdated`
- `ApprovalRequestCreated`
- `ApprovalRequestApproved`
- `ApprovalRequestDenied`
- `ScenarioSimulationRequested`

### Planning Domain (`planning-api`)
- `EnvelopeCreated`
- `EnvelopeUpdated`
- `EnvelopeArchived`
- `EnvelopeRolloverPolicyUpdated`
- `BudgetCreated`
- `BudgetUpdated`
- `BudgetRolloverApplied`
- `RecurringBillCreated`
- `RecurringBillUpdated`
- `RecurringBillDeleted`
- `RecurringBillExecutionPosted`
- `EnvelopeGoalCreated`
- `EnvelopeGoalUpdated`
- `EnvelopeGoalDeleted`

### Automation Domain (`automation-api`)
- `AutomationRuleCreated`
- `AutomationRuleUpdated`
- `AutomationRuleEnabled`
- `AutomationRuleDisabled`
- `AutomationRuleDeleted`
- `TransactionCategorizationApplied`
- `IncomeAllocationApplied`

### Ingestion Domain (`ingestion-api`)
- `TransactionImportPreviewGenerated`
- `TransactionImportCommitted`
- `TransactionImportRowRejected`

### Financial Domain (`financial-api`)
- `PlaidLinkTokenCreated`
- `PlaidPublicTokenExchanged`
- `PlaidAccountLinked`
- `PlaidAccountUnlinked`
- `PlaidTransactionsSynced`
- `PlaidBalancesRefreshed`
- `StripeSetupIntentCreated`
- `StripeFinancialAccountProvisioned`
- `CardVirtualIssued`
- `CardPhysicalIssued`
- `CardFrozen`
- `CardUnfrozen`
- `CardCancelled`
- `CardControlsUpdated`
- `ProviderNotificationDispatchFailed`
- `ProviderNotificationDispatchRetried`

### Reporting Domain (`reporting-api`)
- `ReportingProjectionUpdated`
- `ReportingProjectionReplayStarted`
- `ReportingProjectionReplayCompleted`

## Versioning and Compatibility Rules
### Non-breaking changes
- Add optional fields to `payload`.
- Add new event names.
- Add optional envelope metadata fields.

### Breaking changes
- Remove/rename fields.
- Change existing field type/semantic.
- Repurpose existing event name.

Breaking changes require:
1. New `eventName` or major `schemaVersion`.
2. Compatibility window where producer can emit both versions.
3. Consumer migration validation before old version retirement.

## Producer Rules
- Producers must include all required envelope fields.
- Producers must only emit canonical event names from this catalog.
- Producers must not publish breaking payload changes under same major version.
- Producers must emit deterministic idempotency identity (`eventId` stable per logical event).

## Consumer Rules
- Consumers must ignore unknown optional payload fields.
- Consumers must reject malformed required fields and route to DLQ.
- Consumers must apply idempotency check keyed on `eventId` plus `sourceService`.
- Consumers must log correlation metadata for traceability.

## Migration Guidance
### Additive change flow
1. Introduce optional payload field under same major version.
2. Update consumers to handle field.
3. Promote producer usage once consumer coverage is complete.

### Breaking change flow
1. Publish a new event version (`eventName` alias or major version bump).
2. Run dual-publish for migration window.
3. Validate consumer cutover.
4. Remove legacy emission after compatibility deadline.

## Governance
- Catalog updates are required for any new event contract.
- JSON contract catalog updates are required for any producer payload/routing/eventName changes.
- Event compatibility checks are enforced by contract tests and CI gates (#300).
- This document is source-of-truth until moved to a dedicated contract registry.
