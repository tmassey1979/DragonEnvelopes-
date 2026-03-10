# Issue 301 - Event Replay and Backfill Tooling for Projection Recovery

## Summary
Implemented auditable reporting-projection replay tooling with projection-set and occurred-at range targeting, dry-run support, throughput safeguards, and replay-run history APIs.

## Delivered
- Added replay run persistence model:
  - `ReportProjectionReplayRun` entity
  - EF configuration: `ReportProjectionReplayRunConfiguration`
  - migration: `20260310170549_AddReportProjectionReplayRuns`
- Expanded reporting projection application contracts:
  - `ReportingProjectionReplayRequestDetails`
  - `ReportingProjectionReplayRunDetails`
  - replay response DTO now includes run metadata, filters, safeguards, counters, and status/error fields
- Added projection set constants:
  - `ReportingProjectionSets` (`All`, `EnvelopeBalances`, `Transactions`)
- Enhanced `ReportingProjectionService` replay behavior:
  - deterministic replay target selection by family + projection set + occurred-at range
  - dry-run mode (target analysis without projection mutation)
  - reset scope behavior by targeted outbox ids and projection set
  - replay safeguards (`batchSize`, `maxEvents`, `throttleMilliseconds`) with clamped limits
  - idempotent re-application by replacing existing applied markers for targeted outbox messages
  - audit run persistence with start/end/status/error/counter tracking
  - replay run list/detail query methods
- Updated report projection endpoints in API and Ledger API:
  - `POST /api/v1/reports/projections/replay` now supports targeting/safeguard query parameters
  - added `GET /api/v1/reports/projections/replay-runs`
  - added `GET /api/v1/reports/projections/replay-runs/{replayRunId}`
- Added replay run response contract:
  - `ReportingProjectionReplayRunResponse`
- Updated operations runbook:
  - replay safety defaults, step-by-step recovery workflow, and validation checklist in `docs/operations/reporting-projections.md`
- Added integration coverage for acceptance criteria:
  - dry-run + audit run behavior
  - projection-set targeting + occurred-at range targeting
  - safeguard normalization and `maxEvents` cap behavior

## Validation
- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -c Release --filter ReportingProjectionEndpointIntegrationTests`
  - 5 passed
- `dotnet test DragonEnvelopes.sln -c Release`
  - `DragonEnvelopes.Domain.Tests`: 6 passed
  - `DragonEnvelopes.Application.Tests`: 143 passed
  - `DragonEnvelopes.Desktop.Tests`: 102 passed
  - `DragonEnvelopes.Financial.Api.IntegrationTests`: 6 passed
  - `DragonEnvelopes.Family.Api.IntegrationTests`: 13 passed
  - `DragonEnvelopes.Ledger.Api.IntegrationTests`: 31 passed
  - `DragonEnvelopes.Api.IntegrationTests`: 113 passed
- `./eng/verify-contract-drift.ps1 -RepoRoot .`
- `./eng/verify-event-contract-compatibility.ps1 -RepoRoot .`

## Time Spent
- 2h 20m
