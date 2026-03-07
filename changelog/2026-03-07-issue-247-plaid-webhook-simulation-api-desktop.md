# Issue 247 - Plaid Webhook Simulation API and Desktop Tooling

## Summary
Added Plaid webhook simulation support end-to-end with an API endpoint and desktop UI integration for operational testing.

## Delivered
- Added API endpoint:
  - `POST /api/v1/webhooks/plaid` (anonymous)
  - Parses webhook payload fields:
    - `webhook_type`
    - `webhook_code`
    - `item_id`
  - Resolves family context by `FamilyFinancialProfile.PlaidItemId`.
  - v1 behavior:
    - `TRANSACTIONS` => invokes Plaid transaction sync
    - `BALANCE` => invokes Plaid balance refresh
    - unknown/missing/unsupported cases => deterministic `Ignored` outcome
    - action failures => deterministic `Failed` outcome with trimmed message
  - invalid/non-object JSON payload returns `400`.
- Added new financial contract:
  - `PlaidWebhookProcessResponse`.
- Added desktop data service support:
  - `IFinancialIntegrationDataService.ProcessPlaidWebhookAsync`
  - `FinancialIntegrationDataService.ProcessPlaidWebhookAsync`
- Added desktop Financial Integrations UI and viewmodel wiring:
  - new Plaid payload input + process button + summary panel
  - new `SimulatePlaidWebhookCommand`
  - validation for empty payload
  - summary formatting for webhook outcome/details
- Updated desktop fake service + smoke tests:
  - fake Plaid webhook response/call count support
  - new Plaid simulation smoke test (validation + success path)
- Added API integration tests:
  - invalid JSON => `400`
  - unknown item => `Ignored`
  - supported item with unsupported webhook type => `Ignored` with family context
- Updated settings capability matrix text to reflect Stripe + Plaid webhook simulation support.
- Updated `Codex/TaskLifecycleChecklist.md` active task log for issue `#247`.

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --configuration Release --no-restore`
  - Passed: 84, Failed: 0
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --configuration Release --no-restore`
  - Passed: 97, Failed: 0
