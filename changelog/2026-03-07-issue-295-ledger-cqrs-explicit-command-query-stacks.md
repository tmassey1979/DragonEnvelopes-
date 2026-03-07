# Issue 295 - Ledger CQRS Refactor (Explicit Command/Query Stacks)

## Summary
Refactored Ledger API account/import/transaction endpoint flows to call command/query buses explicitly and added dedicated CQRS handlers so read/write paths are isolated from direct endpoint service coupling.

## Delivered
- Added explicit Ledger CQRS handlers and contracts:
  - Accounts:
    - `CreateAccountCommand` + handler
    - `ListAccountsQuery` + handler
  - Imports:
    - `PreviewTransactionImportQuery` + handler
    - `CommitTransactionImportCommand` + handler
  - Transfers:
    - `CreateEnvelopeTransferCommand` + handler
  - Transactions:
    - `UpdateTransactionCommand` + handler
    - `DeleteTransactionCommand` + handler
    - `RestoreTransactionCommand` + handler
    - `ListDeletedTransactionsQuery` + handler
- Updated Ledger API endpoints to use `ICommandBus` / `IQueryBus` explicitly:
  - `AccountAndTransactionEndpoints.Accounts.cs`
  - `AccountAndTransactionEndpoints.Imports.cs`
  - `AccountAndTransactionEndpoints.Transactions.cs`
- Registered all new handlers in `Application.DependencyInjection`.
- Added CQRS handler test coverage:
  - `tests/DragonEnvelopes.Application.Tests/CqrsLedgerHandlersTests.cs`

## Validation
- `dotnet build src/DragonEnvelopes.Ledger.Api/DragonEnvelopes.Ledger.Api.csproj -c Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -c Release` (139 passed)
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -c Release` (24 passed)

## Time Spent
- 1h 10m
