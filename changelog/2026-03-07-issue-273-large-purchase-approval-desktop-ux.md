# Issue 273 - Large-Purchase Approval Desktop UX

## Summary
Implemented desktop approval UX for large-purchase review workflows.

## Delivered
- Added approval request view model and transaction submission result contract for 202-accepted approval flows.
- Updated transaction data service contracts and implementation to:
  - return submission metadata on transaction create,
  - list approval requests,
  - approve/deny requests.
- Added pending approvals panel in Transactions UI with refresh/approve/deny actions.
- Added approval status badges in transaction grid rows.
- Added parent-role command gating in transactions workspace.
- Added/updated desktop unit tests for:
  - approval-required submission behavior,
  - approval command role gating,
  - transaction status badge behavior.

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -c Release`
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj -c Release`

## Time Spent
- 2h 05m
