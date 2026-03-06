# 2026-03-06 - Issue #140 - Plaid Balance Refresh and Reconciliation

## Summary
- Added on-demand and scheduled Plaid balance refresh.
- Added reconciliation report endpoint to flag balance drift.
- Added persisted drift snapshots and drift-handling operations doc.

## Backend Changes
- Domain:
  - Added `PlaidBalanceSnapshot` audit entity.
- Infrastructure:
  - Added EF config + migration for `plaid_balance_snapshots`.
  - Added `IPlaidBalanceSnapshotRepository` + implementation.
  - Extended account repository with update/save methods for balance correction.
- Application:
  - Added Plaid balance DTOs and service contracts.
  - Added `IPlaidBalanceReconciliationService` / `PlaidBalanceReconciliationService`:
    - refresh family balances from Plaid provider balances
    - auto-correct internal balances for mapped accounts
    - track drift counters and total absolute drift
    - generate reconciliation report with per-account drift flags
    - structured logs for refresh/report operations
  - Added connected-family refresh orchestration for worker use.
- Provider Client:
  - Extended Plaid gateway with `GetAccountBalancesAsync` (`/accounts/balance/get`).
- API:
  - Added routes:
    - `POST /api/v1/families/{familyId}/financial/plaid/refresh-balances`
    - `GET /api/v1/families/{familyId}/financial/plaid/reconciliation`
  - Added scheduled worker `PlaidBalanceRefreshWorker`.

## Documentation
- Added drift workflow doc:
  - `docs/operations/plaid-balance-drift-workflow.md`

## Acceptance Coverage
- Scheduled and on-demand balance refresh supported.
- Reconciliation report returns drift by account.
- Drift handling process documented and auditable via snapshots.
- Family isolation enforced by existing access guards + integration tests.
- Structured telemetry added for refresh/report/worker failures.

## Tests
- Added application tests for:
  - balance refresh correction + snapshot write
  - reconciliation drift flag output
- Added integration auth/isolation tests for family boundary on new Plaid balance routes.

## Validation Run
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --no-build`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --no-build`
