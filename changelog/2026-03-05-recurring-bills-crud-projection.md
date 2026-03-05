# 2026-03-05 - Recurring Bills CRUD and Projection (#82)

## Summary
- Added domain model `RecurringBill` with fields:
  - `Id`, `FamilyId`, `Name`, `Merchant`, `Amount`, `Frequency`, `DayOfMonth`, `StartDate`, `EndDate`, `IsActive`
- Added recurring frequency enum (`Monthly`, `Weekly`, `BiWeekly`).
- Added EF Core configuration and migration for `recurring_bills` with index `(FamilyId, IsActive)`.
- Added contracts under `src/DragonEnvelopes.Contracts/RecurringBills` for create/update/response/projection responses.
- Added application DTOs, repository interface/implementation, and service (`IRecurringBillService`/`RecurringBillService`).
- Added API endpoints:
  - `POST /api/v1/recurring-bills`
  - `GET /api/v1/recurring-bills?familyId=...`
  - `PUT /api/v1/recurring-bills/{recurringBillId}`
  - `DELETE /api/v1/recurring-bills/{recurringBillId}`
  - `GET /api/v1/recurring-bills/projection?familyId=...&from=...&to=...`
- Added validators for recurring bill create/update requests.
- Updated README endpoint list.

## Projection Behavior
- Filters to active recurring bills only.
- Applies start/end date bounds.
- Monthly day handling clamps to month length (e.g., day 31 -> Feb 28/29).
- Results are ordered by due date, then name.

## Tests Added
- `RecurringBillServiceTests` covering:
  - create/update/delete lifecycle
  - monthly day-31 short-month projection edge case
  - start/end date bounds
  - inactive bill exclusion

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release`
- `dotnet ef migrations add AddRecurringBills --project src/DragonEnvelopes.Infrastructure/DragonEnvelopes.Infrastructure.csproj --startup-project src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj`
- `docker compose up -d --build api`
- Authenticated API smoke for create/list/update/projection/delete including day-31 projection check.
