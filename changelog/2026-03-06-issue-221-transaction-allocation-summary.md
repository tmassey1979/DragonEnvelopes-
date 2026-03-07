# Issue 221: Transactions allocation summary display

## Summary
- Added `AllocationDisplay` to `TransactionListItemViewModel` so transaction rows clearly show allocation status:
  - `Split (N)` for split transactions
  - envelope name for single-envelope transactions
  - `Unassigned` when no envelope/splits are present
- Updated Transactions grid column from `Envelope` to `Allocation` using the new display field.
- Added desktop unit tests covering single-envelope, split, and unassigned formatting.

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal`
- `dotnet build DragonEnvelopes.sln -v minimal`
