# 2026-03-05 - Budget Cycle Editor (Create/Update)

## Summary
Added monthly budget create/update workflows to the desktop Budgets page using the budgets API endpoints.

## Changes
- Extended budgets data service with:
  - `GET /budgets/{familyId}/{month}`
  - `POST /budgets`
  - `PUT /budgets/{budgetId}`
- Added `BudgetMonthSummaryData` model and service interface methods.
- Updated `BudgetsViewModel` with create/update editor state and save command.
- Added month-format and non-negative income validation.
- Updated Budgets template to include budget editor panel with create/update action.
- Improved workspace loading behavior so visual section can render even when month budget is missing.

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj -p:OutputPath=bin/TempVerify/` (pass)

## Notes
- `404` budget lookup now maps to create-mode UX.
- Existing visualization remains in place and refreshes after save.
