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
- Replay / backfill:
  - `POST /api/v1/reports/projections/replay?familyId={familyId?}&projectionSet={All|EnvelopeBalances|Transactions}&fromOccurredAtUtc={utc?}&toOccurredAtUtc={utc?}&dryRun={bool?}&resetState={bool?}&batchSize={batchSize?}&maxEvents={maxEvents?}&throttleMilliseconds={ms?}`
- Replay run audit:
  - `GET /api/v1/reports/projections/replay-runs?familyId={familyId?}&take={take?}`
  - `GET /api/v1/reports/projections/replay-runs/{replayRunId}`

### Replay Safety Defaults
- `projectionSet` default: `All`
- `resetState` default: `true`
- `dryRun` default: `false`
- `batchSize` default: `500` (clamped to `1..2000`)
- `maxEvents` default: `50000` (clamped to `1..200000`)
- `throttleMilliseconds` default: `0` (clamped to `0..5000`)

### Recovery Workflow
1. Check projection health with `GET /reports/projections/status` and capture pending, failed, lag, and row counts.
2. Run a dry-run replay with intended filters (`familyId`, `projectionSet`, and optional occurred-at range) to validate target volume.
3. Execute replay with `dryRun=false`, bounded `batchSize`, optional `maxEvents`, and optional throttle.
4. Watch logs for replay failures and review audit records via `/reports/projections/replay-runs`.
5. Re-check status and reporting endpoints. Confirm pending backlog is near zero and failed count is not increasing.
6. If needed, rerun with narrower filters or larger caps after code/data fixes.

### Validation Checklist
- `PendingCount` trends toward zero after replay.
- `FailedCount` remains stable or decreases after remediation.
- `EnvelopeProjectionRowCount` and `TransactionProjectionRowCount` align with expected family data volume.
- `/reports/envelope-balances`, `/reports/monthly-spend`, and `/reports/category-breakdown` return expected values for sampled families.

## Failure Handling
- Projection apply failures are recorded as failed applied-events and excluded from pending scans.
- Use status endpoint (`FailedCount`) plus application logs to identify failed events.
- After fixing code/data issues, run replay for the affected family (or global replay) to rebuild deterministically.
