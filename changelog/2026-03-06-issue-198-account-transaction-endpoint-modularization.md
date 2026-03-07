# Issue #198 - Account/Transaction/Import Endpoint Modularization

## Delivered
- Refactored `AccountAndTransactionEndpoints` into concern-based partial mapping files:
  - `AccountAndTransactionEndpoints.Accounts.cs`
  - `AccountAndTransactionEndpoints.Transactions.cs`
  - `AccountAndTransactionEndpoints.Imports.cs`
- Simplified `AccountAndTransactionEndpoints.cs` to orchestration-only registration.
- Preserved external behavior:
  - same route paths
  - same auth policies
  - same operation names/OpenAPI metadata
  - same entry point (`MapAccountAndTransactionEndpoints`)

## Validation
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
