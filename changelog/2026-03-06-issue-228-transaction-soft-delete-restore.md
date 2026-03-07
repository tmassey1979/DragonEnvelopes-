# Issue #228: Transaction Soft-Delete + Restore Workflow

## Summary
- Converted transaction deletion from hard-delete to soft-delete with retention metadata.
- Added restore and recently-deleted listing APIs with family authorization boundaries.
- Kept balance mutation correctness reversible for envelope and split allocations.

## Delivered
- Domain + persistence:
  - Added `Transaction.DeletedAtUtc` and `Transaction.DeletedByUserId`.
  - Added domain methods `SoftDelete(...)` and `Restore()`.
  - Added EF configuration for soft-delete fields and index.
  - Added migration `AddTransactionSoftDeleteRestore`.
- Application:
  - Updated transaction repository active list query to exclude soft-deleted rows.
  - Added repository method to list deleted transactions by family + bounded window.
  - Updated `TransactionService.DeleteAsync` to soft-delete while reversing envelope/split effects.
  - Added `TransactionService.RestoreAsync` to reapply envelope/split effects and clear delete markers.
  - Added `TransactionService.ListDeletedAsync` with bounded days clamp.
  - Updated transaction DTO/contract mapper path to include optional delete metadata.
- API + Ledger API:
  - Updated delete endpoint to record deleting user id (`sub`) on soft-delete.
  - Added `POST /api/v1/transactions/{id}/restore`.
  - Added `GET /api/v1/transactions/deleted?familyId={id}&days={n}`.
- Reporting:
  - Excluded soft-deleted transactions from reporting transaction set.

## Validation
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -v minimal --filter "FullyQualifiedName~TransactionServiceTests"`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -v minimal --filter "FullyQualifiedName~UserA_Can_SoftDelete_Own_Transaction_And_Rebalance_Envelope|FullyQualifiedName~UserA_Cannot_Delete_FamilyB_Transaction|FullyQualifiedName~UserA_Can_Restore_Own_SoftDeleted_Transaction_And_Reapply_Envelope|FullyQualifiedName~UserA_Cannot_Restore_FamilyB_Transaction|FullyQualifiedName~UserA_Can_List_Recently_Deleted_Transactions_For_Own_Family"`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -v minimal`
- `dotnet build DragonEnvelopes.sln -v minimal`
