# 2026-03-06 - Issue #139 - Plaid Transaction Sync Pipeline

## Summary
- Added cursor-based Plaid transaction sync pipeline with background worker.
- Added family/account mapping support for Plaid account ids.
- Added idempotent dedupe records for synced Plaid transactions.

## Backend Changes
- Domain:
  - Added `PlaidAccountLink` (family Plaid account id -> local account id mapping).
  - Added `PlaidSyncCursor` (cursor tracking per family).
  - Added `PlaidSyncedTransaction` (idempotent dedupe record per Plaid transaction id).
- Infrastructure:
  - Added EF configurations + migration for:
    - `plaid_account_links`
    - `plaid_sync_cursors`
    - `plaid_synced_transactions`
  - Added repositories:
    - `IPlaidAccountLinkRepository` / `PlaidAccountLinkRepository`
    - `IPlaidSyncCursorRepository` / `PlaidSyncCursorRepository`
    - `IPlaidSyncedTransactionRepository` / `PlaidSyncedTransactionRepository`
  - Extended financial profile repository with `ListPlaidConnectedAsync`.
- Application:
  - Added Plaid sync DTOs + models.
  - Added `IPlaidTransactionSyncService` / `PlaidTransactionSyncService`:
    - cursor-based paging (`transactions/sync`)
    - dedupe by Plaid transaction id
    - map synced rows into local `Transaction` model
    - track pulled/inserted/deduped/unmapped counts
    - structured telemetry per sync batch
- Provider Client:
  - Extended `IPlaidGateway` + Plaid gateway with `SyncTransactionsAsync`.
- API:
  - Added routes:
    - `POST /api/v1/families/{familyId}/financial/plaid/account-links`
    - `GET /api/v1/families/{familyId}/financial/plaid/account-links`
    - `POST /api/v1/families/{familyId}/financial/plaid/sync-transactions`
  - Added background worker: `PlaidTransactionSyncWorker`.

## Acceptance Coverage
- Background sync worker pulls transactions by cursor/date-backed payload from Plaid `transactions/sync`.
- Dedupe/idempotency enforced via `plaid_synced_transactions` unique key.
- Synced Plaid rows map into existing local `Transaction` model using configured account links.
- Structured logging added around sync batches and worker failures.

## Tests
- Added application tests:
  - mapped transaction insert + cursor update path
  - dedupe/unmapped accounting behavior
- Added integration auth/isolation tests:
  - cross-family account-link create forbidden
  - cross-family Plaid sync trigger forbidden

## Validation Run
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --no-build`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --no-build`
