# Issue #230: Desktop Transactions Deleted/Restore Workspace

## Summary
- Added desktop UI and data-service support for browsing recently deleted transactions and restoring them.
- Wired deleted transaction metadata through the transaction list view model and API mapping.
- Expanded desktop test coverage for deleted list loading, delete->deleted transition, and restore flow.

## Delivered
- Services:
  - Added `GetDeletedTransactionsAsync(int days, ...)` and `RestoreTransactionAsync(Guid, ...)` to `ITransactionsDataService`.
  - Implemented deleted transaction fetch (`GET transactions/deleted?familyId=...&days=...`) and restore call (`POST transactions/{id}/restore`) in `TransactionsDataService`.
  - Refactored transaction response mapping to include delete metadata (`DeletedAtUtc`, `DeletedByUserId`).
- View models:
  - Added deleted metadata properties to `TransactionListItemViewModel` (`IsDeleted`, `DeletedAtDateDisplay`).
  - Extended `TransactionsViewModel` with deleted list state, selectable deleted item, restore command, and configurable deleted window days.
  - Updated load/delete flows to refresh active + deleted sets consistently.
- UI:
  - Added a "Recently Deleted" section in transactions workspace with days window input, deleted transactions grid, and restore action.
- Tests:
  - Updated `TransactionsViewModelTests` fake service for new data service contract.
  - Added tests for deleted list load behavior and restore workflow.
  - Extended delete behavior assertion to confirm movement to deleted list.

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
