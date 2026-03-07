# Issue #192 - Dashboard Live Data Integration

## Delivered
- Replaced hard-coded dashboard placeholder values with live family data.
- Added dashboard workspace contracts:
  - `IDashboardDataService`
  - `DashboardWorkspaceData`
  - `DashboardRecentTransactionData`
- Implemented `DashboardDataService` to aggregate dashboard inputs from existing API routes:
  - `GET /api/v1/accounts?familyId=...`
  - `GET /api/v1/envelopes?familyId=...`
  - `GET /api/v1/reports/monthly-spend?familyId=...`
  - `GET /api/v1/reports/remaining-budget?familyId=...`
  - `GET /api/v1/transactions?accountId=...` (across family accounts)
- Updated `DashboardViewModel` to:
  - build KPI cards from live metrics (net worth, cash balance, monthly spend, budget health)
  - load recent transaction rows from live transaction activity
  - handle empty and error states deterministically
- Added optional `autoLoad` constructor flag in `DashboardViewModel` for deterministic unit tests.
- Wired dashboard route to use `DashboardDataService` in `RouteRegistry`.
- Added dashboard desktop tests:
  - success workspace load
  - empty workspace behavior
  - service failure behavior

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --filter "DashboardViewModelTests" -v minimal`
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
