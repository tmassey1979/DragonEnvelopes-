# Issue #184 - Manual Recurring Auto-Post Run API and Desktop Control

## Delivered
- Extracted recurring auto-post logic into reusable API service:
  - `IRecurringAutoPostService`
  - `RecurringAutoPostService`
  - shared run summary models (`RecurringAutoPostRunSummary`, `RecurringAutoPostExecutionItem`)
- Refactored `RecurringBillAutoPostWorker` to call shared service to avoid behavior drift.
- Added recurring auto-post run contracts:
  - `RecurringAutoPostRunResponse`
  - `RecurringAutoPostExecutionResponse`
- Added authenticated parent-only manual run endpoint:
  - `POST /api/v1/families/{familyId}/recurring-bills/auto-post/run?dueDate=...`
  - enforces family-access guard.
- Added desktop recurring bills manual control:
  - new data service method `RunAutoPostAsync(...)`
  - new run result/execution view models
  - new UI section with `Run Auto-Post Now` button and execution result grid.
- Added API integration tests for:
  - allowed manual run on owned family
  - forbidden run on non-owned family.

## Validation
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass

## Notes
- Existing idempotency behavior remains based on `(RecurringBillId, DueDate)` via execution repository checks.
