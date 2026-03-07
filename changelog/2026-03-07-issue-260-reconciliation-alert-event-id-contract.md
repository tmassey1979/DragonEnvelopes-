# 2026-03-07 Issue #260 - Provider timeline reconciliation alert id contract

## Summary
- Added nullable `ReconciliationAlertEventId` to `ProviderTimelineEventResponse` so reconciliation timeline records have an explicit event id field.
- Updated provider activity timeline mapping to populate `ReconciliationAlertEventId` for `PlaidReconciliation` events and stop overloading `PlaidWebhookEventId`.
- Updated desktop provider timeline model and routing logic to use `ReconciliationAlertEventId` when loading reconciliation detail.
- Added desktop timeline grid column for reconciliation alert ids.

## Validation
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --filter "FullyQualifiedName~Provider_Activity_Timeline|FullyQualifiedName~Provider_Timeline_Event_Detail" --nologo`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --filter "FullyQualifiedName~FinancialIntegrationsViewModelSmokeTests" --nologo`
- `dotnet build DragonEnvelopes.sln --nologo`

## Notes
- Existing Stripe/Plaid webhook and notification replay behavior remains unchanged.
- Added coverage for reconciliation-detail lookup path using the dedicated id field.
