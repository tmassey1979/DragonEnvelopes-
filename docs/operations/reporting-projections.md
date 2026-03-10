# Reporting Projections Operations Guide

## Purpose
Reporting endpoints (`/reports/envelope-balances`, `/reports/monthly-spend`, `/reports/category-breakdown`) are backed by event-driven read models built from integration outbox events.

## Worker
- Service: `ReportingProjectionWorker` (Ledger API)
- Projector: `ReportingProjectionService`
- Relevant routing keys:
  - `planning.envelope.created.v1`
  - `planning.envelope.updated.v1`
  - `planning.envelope.archived.v1`
  - `ledger.transaction.created.v1`
  - `ledger.transaction.updated.v1`
  - `ledger.transaction.deleted.v1`
  - `ledger.transaction.restored.v1`

## Configuration
`src/DragonEnvelopes.Ledger.Api/appsettings*.json`

```json
"Reporting": {
  "ProjectionWorker": {
    "Enabled": true,
    "PollIntervalSeconds": 5,
    "BatchSize": 100,
    "BacklogWarningThreshold": 100
  }
}
```

## Freshness / Lag Expectations
- Target steady-state lag: `<= 30 seconds`.
- Warning threshold: backlog count above `BacklogWarningThreshold` logs warnings.
- Poll interval and batch size directly impact lag under load.

## Manual Operations
- Status:
  - `GET /api/v1/reports/projections/status?familyId={familyId?}`
- Replay / rebuild:
  - `POST /api/v1/reports/projections/replay?familyId={familyId?}&batchSize={batchSize?}`

Replay clears projection rows and applied markers for the selected family scope (or all families when omitted), then rebuilds from outbox event history.

## Failure Handling
- Projection apply failures are recorded as failed applied-events and excluded from pending scans.
- Use status endpoint (`FailedCount`) plus application logs to identify failed events.
- After fixing code/data issues, run replay for the affected family (or global replay) to rebuild deterministically.
