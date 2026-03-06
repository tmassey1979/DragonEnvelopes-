# Changelog - 2026-03-05 - Issue #113

## Summary
Added recurring-bill background auto-post worker that creates due transactions and records execution outcomes through the idempotency ledger.

## Changes
- Added hosted service:
  - `RecurringBillAutoPostWorker`
  - scans active recurring bills on a periodic loop
  - determines due bills by frequency/date rules
  - posts transactions for due bills using `ITransactionService`
  - records execution result (`Posted`, `SkippedNoAccount`, `Failed`) in execution ledger
- Added startup wiring:
  - registers worker as hosted service in non-`Testing` environments

## Notes
- Worker currently posts to the first available family account (ordered by account id).
- Worker uses recurring execution ledger helper (`HasExecutionAsync`) to avoid duplicate posting for same bill+due date.

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`
- Result: build succeeded; integration tests passed (13/13).
