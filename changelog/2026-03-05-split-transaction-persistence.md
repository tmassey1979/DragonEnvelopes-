# 2026-03-05 - split transaction persistence and sum-validation support

## Summary
- Added persistent split transaction storage via new `transaction_splits` table and EF configuration.
- Added `TransactionSplitEntry` domain entity for persisted split rows.
- Updated transaction service create flow to persist split rows and return them in API responses.
- Updated transaction list flow to hydrate split rows per transaction.
- Kept existing split sum-validation behavior from request validators and domain logic.
- Added notes validation max length for split request payloads.
- Added/updated transaction service tests for split persistence and envelope balance mutation behavior.

## Files Changed
- src/DragonEnvelopes.Domain/Entities/TransactionSplitEntry.cs
- src/DragonEnvelopes.Infrastructure/Configuration/TransactionSplitEntryConfiguration.cs
- src/DragonEnvelopes.Infrastructure/Persistence/DragonEnvelopesDbContext.cs
- src/DragonEnvelopes.Infrastructure/Repositories/TransactionRepository.cs
- src/DragonEnvelopes.Infrastructure/Persistence/Migrations/20260305204718_AddTransactionSplits.cs
- src/DragonEnvelopes.Infrastructure/Persistence/Migrations/20260305204718_AddTransactionSplits.Designer.cs
- src/DragonEnvelopes.Infrastructure/Persistence/Migrations/DragonEnvelopesDbContextModelSnapshot.cs
- src/DragonEnvelopes.Application/DTOs/TransactionDetails.cs
- src/DragonEnvelopes.Application/Interfaces/ITransactionRepository.cs
- src/DragonEnvelopes.Application/Services/ITransactionService.cs
- src/DragonEnvelopes.Application/Services/TransactionService.cs
- src/DragonEnvelopes.Api/Program.cs
- src/DragonEnvelopes.Api/CrossCutting/Validation/Validators/RequestValidators.cs
- tests/DragonEnvelopes.Application.Tests/TransactionServiceTests.cs

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release`
- `dotnet ef migrations add AddTransactionSplits ...` (already applied to codebase)
- `docker compose up -d --build api`
- End-to-end smoke: create split transaction and verify response/list include split rows and linked envelope balances update
