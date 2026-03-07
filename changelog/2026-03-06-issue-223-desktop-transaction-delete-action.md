# Issue 223: Desktop transaction delete action

## Summary
- Added desktop transaction delete API support:
  - `ITransactionsDataService.DeleteTransactionAsync`
  - `TransactionsDataService.DeleteTransactionAsync` calling `DELETE /api/v1/transactions/{transactionId}`
- Added `DeleteSelectedTransactionCommand` to `TransactionsViewModel`.
- Delete flow now:
  - validates selected transaction
  - calls API delete
  - exits edit mode/reset editor when needed
  - reloads transaction list
  - reports `"Transaction deleted."` status on success
- Updated Transactions UI controls to include `Delete Selected` next to edit actions.
- Added unit test coverage for delete command behavior in edit mode.

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal`
- `dotnet build DragonEnvelopes.sln -v minimal`
