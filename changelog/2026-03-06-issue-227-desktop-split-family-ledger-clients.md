# Issue #227: Desktop Split Family/Ledger API Clients

## Summary
- Introduced desktop API client split routing with explicit Family and Ledger base URLs and fallback to monolith base URL.
- Routed desktop domains to the correct client targets by concern.
- Added missing split-API parity required for desktop startup/context and recurring routing.

## Delivered
- Desktop API client abstraction:
  - Extended `ApiClientOptions` with:
    - `FamilyBaseUrl` (`DRAGONENVELOPES_FAMILY_API_BASE_URL`)
    - `LedgerBaseUrl` (`DRAGONENVELOPES_LEDGER_API_BASE_URL`)
  - Added fallback behavior to `BaseUrl` when split URLs are not provided.
  - Added `DesktopApiClientFactory` and `DesktopApiClients` (`Family`, `Ledger`).
- Desktop wiring:
  - Updated `MainWindowViewModel` default composition to build Family + Ledger clients.
  - Updated `RouteRegistry` to use Family client for family/onboarding/member/settings concerns and Ledger client for account/transaction/envelope/budget/report/automation/import/recurring concerns.
  - Updated `FamilyAccountServiceFactory` to use Family API client path.
- Family API parity needed for desktop context and invite onboarding:
  - Added `/api/v1/auth/me`, `/api/v1/auth/parent-only`, `/api/v1/system/health`, `/api/v1/system/version`.
  - Added invite endpoints:
    - `POST /api/v1/families/invites/redeem`
    - `POST /api/v1/families/invites/register`
  - Added mapper/validators for redeem/register payloads.
- Ledger API parity for recurring route target:
  - Added recurring planning endpoint module and mapping:
    - CRUD/projection/executions
    - `POST /api/v1/families/{familyId}/recurring-bills/auto-post/run`
  - Added recurring auto-post service registration and service implementation in Ledger API.
- Documentation:
  - Updated README desktop env vars with split URL overrides and fallback behavior.

## Verification
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal`
- `dotnet test tests/DragonEnvelopes.Family.Api.IntegrationTests/DragonEnvelopes.Family.Api.IntegrationTests.csproj -v minimal`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -v minimal`
- `dotnet build DragonEnvelopes.sln -v minimal`
