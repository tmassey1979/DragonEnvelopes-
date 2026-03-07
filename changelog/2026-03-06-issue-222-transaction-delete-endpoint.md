# Issue 222: Transactions delete endpoint + rebalance

## Summary
- Added transaction delete capability to application layer:
  - `ITransactionService.DeleteAsync`
  - `ITransactionRepository.DeleteTransactionAsync`
- Implemented `TransactionService.DeleteAsync` with envelope rebalance reversal:
  - reverses single-envelope allocations/spend before delete
  - reverses split allocations/spend for each split envelope before delete
  - removes transaction splits and transaction, then saves changes
- Added `DELETE /api/v1/transactions/{transactionId}` to:
  - monolith API (`DragonEnvelopes.Api`)
  - Ledger API (`DragonEnvelopes.Ledger.Api`)
- Endpoint authorization follows existing family access guard pattern.

## Tests Added
- Application tests (`TransactionServiceTests`):
  - single-envelope delete rebalance
  - split delete rebalance
  - missing transaction throws
- Monolith integration tests (`AuthIsolationIntegrationTests`):
  - authorized family delete + envelope rebalance
  - cross-family delete forbidden
- Ledger integration tests (`LedgerApiSmokeTests`):
  - own-family transaction delete allowed
  - other-family transaction delete forbidden

## Validation
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -v minimal --filter "FullyQualifiedName~TransactionServiceTests"`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -v minimal`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -v minimal --filter "FullyQualifiedName~UserA_Can_Delete_Own_Transaction_And_Rebalance_Envelope|FullyQualifiedName~UserA_Cannot_Delete_FamilyB_Transaction"`
- `dotnet build DragonEnvelopes.sln -v minimal`
