# Issue 298 - Reporting Projections Service from Domain Events

## Summary
Implemented event-driven reporting read models with projection worker + replay support, and routed reporting queries to projection storage (with compatibility fallback when projection data is not yet available).

## Delivered
- Added projection entities/tables:
  - `report_envelope_balance_projections`
  - `report_transaction_projections`
  - `report_projection_applied_events`
- Added migration:
  - `20260310145227_AddReportingProjections`
- Implemented `IReportingProjectionService` with:
  - pending event projection from outbox messages
  - idempotent applied-event tracking
  - family-scoped or global replay/rebuild
  - projection status (pending/applied/failed counts + lag)
- Added background worker in Ledger API:
  - `ReportingProjectionWorker`
  - `ReportingProjectionWorkerOptions`
  - config section `Reporting:ProjectionWorker`
- Added projection admin/reporting endpoints:
  - `GET /api/v1/reports/projections/status`
  - `POST /api/v1/reports/projections/replay`
  - mirrored in both `Ledger.Api` and compatibility `Api` routing sets
- Updated `ReportingRepository` to read from projection tables when projection data exists, with fallback to direct tables for compatibility.
- Added docs:
  - `docs/operations/reporting-projections.md` (freshness/lag target + operations runbook)
- Added integration coverage:
  - `ReportingProjectionEndpointIntegrationTests` verifying replay builds correct report outputs and family access enforcement.

## Validation
- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -c Release --no-build` (26 passed)
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -c Release --no-build` (113 passed)
- `dotnet test DragonEnvelopes.sln -c Release --no-build` (399 passed)

## Time Spent
- 2h 05m
