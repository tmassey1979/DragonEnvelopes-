# 2026-03-06 - Issues #151 and #152

## Summary
- Added automated desktop smoke coverage for the Financial Integrations workspace.
- Added a family-scoped provider activity and health API plus desktop health panel and recovery actions.

## Issue #151 - Desktop QA smoke automation
- Added `tests/DragonEnvelopes.Desktop.Tests` (xUnit, net8.0-windows) and included it in `DragonEnvelopes.sln`.
- Implemented deterministic mocked smoke tests for `FinancialIntegrationsViewModel` covering:
  - workspace load
  - status refresh and Plaid sync flow
  - native Plaid link flow command path
  - Stripe setup intent validation/action flow
  - card action commands + control save/evaluation flow
  - failure-state action messaging
- Added manual fallback checklist:
  - `docs/qa/desktop-financial-integrations-smoke-checklist.md`

## Issue #152 - Provider activity and health panel
- Added contracts:
  - `ProviderActivityHealthResponse`
  - `StripeWebhookActivityResponse`
  - `SpendNotificationDispatchStatusResponse`
- Added API endpoint:
  - `GET /api/v1/families/{familyId}/financial/provider-activity`
  - Family-scoped authorization guard applied.
  - Returns latest Plaid sync/refresh timestamps, drift metrics, latest Stripe webhook outcome, and notification dispatch status.
- Added desktop data service support:
  - `IFinancialIntegrationDataService.GetProviderActivityHealthAsync`
  - `FinancialIntegrationDataService.GetProviderActivityHealthAsync`
- Added desktop ViewModel bindings/refresh flow:
  - `RefreshProviderActivityCommand`
  - provider activity summary properties
  - provider activity loaded in initial workspace load and post sync/refresh actions
- Added desktop UI panel in Financial Integrations template with:
  - provider activity metrics
  - non-sensitive error summary visibility
  - recovery actions (`Retry Plaid Sync`, `Refresh Balances`, `Refresh Issuance`)
- Added API integration tests for provider activity auth boundaries.

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Domain.Tests/DragonEnvelopes.Domain.Tests.csproj --no-build`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --no-build`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --no-build`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --no-build`
