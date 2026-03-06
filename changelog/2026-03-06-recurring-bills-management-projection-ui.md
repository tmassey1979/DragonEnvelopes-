# 2026-03-06 - Recurring Bills Management and Projection UI

## Summary
Implemented a dedicated recurring bills desktop page with CRUD editor and date-range projection viewer.

## Changes
- Added recurring bills data service + interface with API bindings:
  - `GET /recurring-bills?familyId=...`
  - `POST /recurring-bills`
  - `PUT /recurring-bills/{id}`
  - `DELETE /recurring-bills/{id}`
  - `GET /recurring-bills/projection?familyId=...&from=...&to=...`
- Added recurring bill view models for list and projection rows.
- Added `RecurringBillsViewModel` with:
  - create/update/delete workflows
  - draft validation and edit-state handling
  - projection date range load and display
  - loading/error/empty behaviors
- Added `/recurring-bills` route and full XAML template.

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj -p:OutputPath=bin/TempVerify/` (pass)

## Notes
- Uses active family context (`IFamilyContext`) for all requests.
- Projection list is ordered by due date then bill name.
