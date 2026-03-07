# Issue #206 - Ledger Account/Transaction Endpoint Modularization

## Delivered
- Split `src/DragonEnvelopes.Ledger.Api/Endpoints/AccountAndTransactionEndpoints.cs` into concern-focused partial files:
  - `src/DragonEnvelopes.Ledger.Api/Endpoints/AccountAndTransactionEndpoints.Accounts.cs`
  - `src/DragonEnvelopes.Ledger.Api/Endpoints/AccountAndTransactionEndpoints.Transactions.cs`
  - `src/DragonEnvelopes.Ledger.Api/Endpoints/AccountAndTransactionEndpoints.Imports.cs`
- Reduced `AccountAndTransactionEndpoints.cs` to an aggregator that composes concern mappers.
- Preserved existing routes, route names, auth policies, and response behavior.

## Validation
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
