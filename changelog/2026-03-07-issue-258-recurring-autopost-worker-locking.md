# Issue 258: Recurring Bills Scheduled Auto-Post Worker

## Summary
- Hardened recurring auto-post scheduling with explicit worker configuration, lock guarding, and structured execution telemetry.
- Preserved existing manual recurring auto-post endpoint behavior while adding idempotency and duplicate-acquisition validation tests.

## Backend Changes
- Added recurring worker options (`RecurringAutoPostWorkerOptions`):
  - `Enabled`
  - `PollIntervalMinutes`
  - `UseDistributedLock`
  - `DistributedLockKey`
- Added lock abstraction:
  - `IRecurringAutoPostWorkerLock`
  - `RecurringAutoPostWorkerLock`
- Lock behavior:
  - Uses PostgreSQL advisory lock (`pg_try_advisory_lock`) when running on Npgsql for cross-instance duplicate-run prevention.
  - Falls back to in-process lock for non-Postgres providers (test/in-memory safety).
- Updated `RecurringBillAutoPostWorker`:
  - honors options-driven enable/disable and polling cadence.
  - acquires worker lock before each run and skips cycle when lock is unavailable.
  - emits structured cycle summary logs with duration metrics.
- DI/bootstrap updates:
  - registers options and lock service.
  - adds `RecurringAutoPost` configuration parsing.
- Added config defaults to API appsettings:
  - `src/DragonEnvelopes.Api/appsettings.json`
  - `src/DragonEnvelopes.Api/appsettings.Development.json`

## Tests
- Added/updated integration coverage in `AuthIsolationIntegrationTests`:
  - manual recurring auto-post idempotency for same family + due date.
  - lock duplicate-acquisition prevention semantics.

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --filter "Manual_Recurring_AutoPost_Is_Idempotent_For_Same_Family_And_DueDate|Recurring_AutoPost_Worker_Lock_Prevents_Concurrent_Acquisition|UserA_Can_Run_Manual_Recurring_AutoPost_For_Own_Family"`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --filter "Recurring_AutoPost|Run_Manual_Recurring_AutoPost"`
