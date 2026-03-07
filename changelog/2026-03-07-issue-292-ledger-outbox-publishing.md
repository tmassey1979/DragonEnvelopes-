# Issue 292 - Ledger Service Outbox Publishing

## Summary
Implemented transactional outbox publishing for Ledger transaction and approval write paths, plus a Ledger outbox dispatcher worker and integration coverage for retry + source isolation semantics.

## Delivered
- Added Ledger event contracts and routing keys:
  - transaction lifecycle: created, updated, deleted, restored
  - approval lifecycle: request created, approved, denied
- Migrated transaction event emission to outbox-based persistence:
  - removed direct publish from `CreateTransactionCommandHandler`
  - `TransactionService` now writes outbox rows atomically with transaction mutations
  - create/update/delete/restore flows enqueue Ledger outbox events
- Added approval workflow outbox emission:
  - blocked/pending create paths enqueue `ApprovalRequestCreated`
  - approve path enqueues `ApprovalRequestApproved`
  - deny path enqueues `ApprovalRequestDenied`
- Updated repository + dispatch behavior for service isolation:
  - outbox dispatch now filters by `SourceService`
  - family worker dispatches only `family-api` messages
  - ledger worker dispatches only `ledger-api` messages
- Added Ledger runtime worker:
  - `LedgerOutboxDispatchWorker` with poll/batch/backlog settings
  - `Messaging:Outbox` and `Messaging:RabbitMq:SourceService` config wiring in Ledger API
- Updated transaction repository save semantics:
  - `AddTransactionAsync` now defers persistence to explicit `SaveChangesAsync` for atomic outbox commits
- Added tests:
  - updated CQRS and transaction tests for new outbox/save behavior
  - updated family outbox tests for source filtering
  - ledger smoke tests for transaction/approval outbox emission
  - new ledger integration test for retry + source isolation dispatch semantics
- Fixed approval entity materialization bug:
  - relaxed constructor status guard to allow EF materialization of resolved states (`Approved`/`Denied`)

## Validation
- `dotnet build src/DragonEnvelopes.Application/DragonEnvelopes.Application.csproj -c Release`
- `dotnet build src/DragonEnvelopes.Infrastructure/DragonEnvelopes.Infrastructure.csproj -c Release`
- `dotnet build src/DragonEnvelopes.Family.Api/DragonEnvelopes.Family.Api.csproj -c Release`
- `dotnet build src/DragonEnvelopes.Ledger.Api/DragonEnvelopes.Ledger.Api.csproj -c Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -c Release`
- `dotnet test tests/DragonEnvelopes.Family.Api.IntegrationTests/DragonEnvelopes.Family.Api.IntegrationTests.csproj -c Release`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -c Release`

## Time Spent
- 2h 05m
