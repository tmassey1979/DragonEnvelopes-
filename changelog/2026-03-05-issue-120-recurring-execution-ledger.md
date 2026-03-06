# Changelog - 2026-03-05 - Issue #120

## Summary
Added recurring bill execution ledger persistence and idempotency-check repository helpers.

## Changes
- Added domain entity:
  - `RecurringBillExecution` (bill/family/due-date/result/transaction tracking)
- Added EF configuration:
  - `RecurringBillExecutionConfiguration`
  - unique index on `(RecurringBillId, DueDate)`
  - family/date index for queryability
- Added DbContext support:
  - `DbSet<RecurringBillExecution>`
- Added repository interface/implementation:
  - `IRecurringBillExecutionRepository`
  - `RecurringBillExecutionRepository`
  - helper methods for `HasExecutionAsync` and execution ledger reads/writes
- Added DI registration for execution repository.
- Added migration:
  - `AddRecurringBillExecutions`
- Added integration test for idempotency helper semantics:
  - verifies `HasExecutionAsync` false->true transition after add

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`
- Result: build succeeded; integration tests passed (13/13).
