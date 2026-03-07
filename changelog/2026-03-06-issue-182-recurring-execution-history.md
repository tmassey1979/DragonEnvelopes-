# Issue #182 - Recurring Execution History API and Desktop Diagnostics

## Delivered
- Added recurring execution response contract: `RecurringBillExecutionResponse`.
- Added application DTO/service support for execution history:
  - `RecurringBillExecutionDetails`
  - `IRecurringBillService.ListExecutionsAsync(...)`
  - deterministic idempotency key format: `<recurringBillId:N>:<dueDate:yyyy-MM-dd>`
- Added API endpoint:
  - `GET /api/v1/recurring-bills/{recurringBillId}/executions?take={n}`
  - guarded by existing family authorization boundary.
- Added desktop recurring bill diagnostics:
  - data service method to fetch execution history
  - view model state + refresh command
  - UI panel in recurring bills screen showing recent executions, result, transaction id, idempotency key, and notes.

## Tests
- Updated and added application tests in `RecurringBillServiceTests` for execution history behavior.
- Added API auth/isolation integration coverage for recurring execution history endpoint.
- Validation command:
  - `dotnet test DragonEnvelopes.sln -v minimal`
  - Result: pass (Domain/Application/Desktop/API integration suites all green).

## Notes
- Reused existing `RecurringBillExecution` persistence model; no schema migration required.
- Trimmed execution notes for safe UI display via service mapping.
