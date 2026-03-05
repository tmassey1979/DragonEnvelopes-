# 2026-03-05 - Transactions Grid with Filtering and Sorting (#63)

## Summary
- Implemented a dedicated Transactions page in WPF (replacing placeholder content).
- Added account-scoped transaction loading from backend API.
- Added filter controls:
  - merchant/description text filter
  - category text filter
  - include/exclude uncategorized toggle
- Added sorting controls with direction toggle:
  - Date
  - Merchant
  - Amount
  - Category
- Added sortable transaction grid view with account selector and reload action.

## New Desktop Components
- `TransactionsViewModel`
- `TransactionListItemViewModel`
- `ITransactionsDataService`
- `TransactionsDataService`

## Navigation/Template Wiring
- `/transactions` route now binds to `TransactionsViewModel`.
- Added `DataTemplate` for transactions workspace in shell templates.

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release`
