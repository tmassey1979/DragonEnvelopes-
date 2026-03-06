# 2026-03-06 - Reports Expansion (Envelope, Monthly Spend, Category)

## Summary
Expanded desktop Reports page to consume core reporting endpoints and display envelope balances, monthly spend trend, and category breakdown.

## Changes
- Updated reports data service to use family-scoped reporting endpoints:
  - `GET /reports/remaining-budget`
  - `GET /reports/envelope-balances`
  - `GET /reports/monthly-spend`
  - `GET /reports/category-breakdown`
- Added report workspace data models for envelope/monthly/category sections.
- Updated `ReportsViewModel`:
  - Added date-range filters (`RangeFrom`, `RangeTo`)
  - Added data collections for envelope balances, monthly spend, category breakdown
  - Added report workspace loading and transformation logic
- Added report row viewmodels for all three sections.
- Expanded Reports XAML template to render:
  - KPI summary tiles
  - Monthly spend table
  - Category breakdown table
  - Envelope balances table

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj -p:OutputPath=bin/TempVerify/` (pass)

## Notes
- Report service now requires active family context.
- Summary values are derived from remaining-budget response where available.
