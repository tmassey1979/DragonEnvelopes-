# Issue 250 - Provider Activity Plaid Timeline Persistence

## Summary
Persisted Plaid webhook processing events and surfaced them in provider activity timeline APIs and desktop filtering.

## Delivered
- Added `PlaidWebhookEvent` domain entity with invariants for webhook lifecycle metadata.
- Added EF Core mapping and `DbSet` for `plaid_webhook_events`.
- Added migration `AddPlaidWebhookEvents` with indexes for family/time, item/time, and type/time lookups.
- Updated `/api/v1/webhooks/plaid` processing to persist every parsed webhook outcome (`Processed`, `Ignored`, `Failed`) with payload and timestamps.
- Updated provider activity timeline endpoint to:
  - accept `source=PlaidWebhook` filter,
  - include Plaid webhook records in merged timeline responses,
  - apply optional status filtering to Plaid events.
- Updated desktop provider timeline source filter options to include `Plaid Webhooks`.
- Extended API integration tests to verify:
  - Family A timeline includes plaid events and excludes Family B plaid data.
  - `source=PlaidWebhook` filtering works.
  - Plaid processed and failed webhook paths persist records in the database.

## Validation
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --configuration Release --no-restore`
  - Passed: 100, Failed: 0
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --configuration Release --no-restore`
  - Passed: 84, Failed: 0
