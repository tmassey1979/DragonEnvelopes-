# 2026-03-05 - Budget Allocation Visualization Page

## Summary
Implemented a dedicated desktop Budgets page with API-backed allocation visualization for month-level and envelope-level budget coverage.

## Changes
- Added new `/budgets` route to desktop navigation.
- Added `BudgetsViewModel` with month filter, include-archived toggle, and loading/error/empty states.
- Added `BudgetsDataService` and `IBudgetsDataService` to read:
  - `GET /api/v1/reports/remaining-budget`
  - `GET /api/v1/reports/envelope-balances`
- Added budget workspace data models for summary + envelope rows.
- Added budget allocation template to render:
  - Total income, allocated, remaining, allocation coverage
  - Envelope allocation table with monthly budget/current balance and percent columns

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj` (pass)

## Notes
- Archived envelopes are client-filtered using the envelope balance report’s `IsArchived` flag.
- If no budget exists for the selected month, the page intentionally renders empty state.
