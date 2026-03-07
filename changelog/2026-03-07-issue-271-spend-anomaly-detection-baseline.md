# 2026-03-07 - Issue #271 - Spend Anomaly Detection Baseline

## Summary
- Added anomaly domain/persistence model:
  - `SpendAnomalyEvent` entity with severity score, reason, baseline metrics, and transaction linkage.
  - EF configuration `SpendAnomalyEventConfiguration`.
  - `DbSet<SpendAnomalyEvent>` in `DragonEnvelopesDbContext`.
  - migration `AddSpendAnomalyEvents`.
- Added anomaly repository and service contracts:
  - `ISpendAnomalyEventRepository`
  - `ISpendAnomalyService`
- Added deterministic baseline heuristic service:
  - `SpendAnomalyService`
  - merchant deviation path: z-score threshold
  - fallback family deviation path: ratio threshold
  - deterministic options model: `SpendAnomalyDetectionOptions`
- Added configurable threshold wiring in API bootstrap:
  - `SpendAnomalies:*` config keys supported in Ledger API and monolith API bootstrap.
- Integrated detection into transaction posting path:
  - `TransactionService.CreateAsync` now calls anomaly detection after successful transaction persistence for spend transactions (`amount < 0`).
- Added anomaly API contract and endpoint:
  - contract: `SpendAnomalyEventResponse`
  - endpoint: `GET /api/v1/spend-anomalies?familyId=...&take=...`
  - endpoint includes family access guard.
- Added mapper support:
  - `EndpointMappers.MapSpendAnomalyEventResponse`
- Added tests:
  - application tests for true/false positive detection:
    - `SpendAnomalyServiceTests`
  - ledger integration auth/isolation test for anomaly list endpoint.

## Validation
- `dotnet build DragonEnvelopes.sln -c Release --nologo`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -c Release --nologo`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -c Release --nologo`

## Notes
- Heuristic is intentionally deterministic and configurable; no ML dependency.
- Detection is non-invasive to routing and uses existing transaction creation workflow.
