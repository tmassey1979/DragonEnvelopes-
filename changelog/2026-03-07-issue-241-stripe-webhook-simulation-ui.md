# Issue 241 - Stripe Webhook Simulation UI

## Summary
Added a Stripe webhook simulation tool to the desktop Financial Integrations workspace so webhook endpoint operations can be exercised directly from the app.

## Delivered
- Extended desktop financial integrations service contract:
  - Added `ProcessStripeWebhookAsync(payload, stripeSignature)` to `IFinancialIntegrationDataService`.
  - Implemented call in `FinancialIntegrationDataService` targeting `POST /api/v1/webhooks/stripe` with optional `Stripe-Signature` header.
- Extended `FinancialIntegrationsViewModel`:
  - Added `SimulateStripeWebhookCommand`.
  - Added bound fields for webhook payload and optional signature.
  - Added simulation summary rendering (`Outcome`, `EventType`, `EventId`, `Message`).
  - Added validation for required payload and operation status updates.
  - Refreshes provider health/timeline/failed notification views after simulation.
- Updated Financial Integrations XAML template:
  - Added “Stripe Webhook Simulation” panel with payload/signature inputs, process action, and result summary.
- Updated capability matrix in `SettingsViewModel`:
  - `Webhook endpoint operations` moved from `Partial` to `Available`.
- Added/updated desktop tests:
  - `FakeFinancialIntegrationDataService` now supports webhook simulation call/count and response shaping.
  - Added smoke test coverage for validation and successful simulation result handling.

## Validation
- `dotnet build DragonEnvelopes.sln -v minimal`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal --no-build`
- `./eng/verify-contract-drift.ps1 -RepoRoot .`
