# Issue 291 - Family Service Outbox Publishing

## Summary
Implemented transactional outbox publishing for Family domain write paths, including a Family API background dispatcher with retry/backoff behavior and backlog telemetry.

## Delivered
- Added integration outbox persistence model:
  - `IntegrationOutboxMessage` domain entity
  - EF configuration and migration (`integration_outbox_messages`)
  - repository abstraction + implementation
- Updated Family domain write paths to atomically persist state + outbox events in the same save transaction:
  - `FamilyCreated`
  - `FamilyMemberAdded`
  - `FamilyMemberRemoved`
  - `FamilyInviteAccepted` (accept/redeem paths)
- Refactored `FamilyRepository` and `FamilyInviteRepository` to defer commits to explicit `SaveChangesAsync`, enabling atomic multi-entity writes.
- Added outbox dispatch pipeline:
  - `IIntegrationOutboxDispatchService` + implementation with retry scheduling and exponential backoff
  - Family API hosted worker (`FamilyOutboxDispatchWorker`) with polling, batching, and backlog warning logs
  - RabbitMQ outbox envelope publisher and no-op fallback publisher
- Added Family integration event contracts + routing keys for the new Family outbox events.
- Added/updated tests:
  - application tests for outbox dispatch success/failure/no-work scenarios
  - family integration tests for atomic outbox persistence and retry-then-dispatch behavior
  - adjusted family service/import tests for explicit save semantics

## Validation
- `dotnet build src/DragonEnvelopes.Application/DragonEnvelopes.Application.csproj -c Release`
- `dotnet build src/DragonEnvelopes.Infrastructure/DragonEnvelopes.Infrastructure.csproj -c Release`
- `dotnet build src/DragonEnvelopes.Family.Api/DragonEnvelopes.Family.Api.csproj -c Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -c Release`
- `dotnet test tests/DragonEnvelopes.Family.Api.IntegrationTests/DragonEnvelopes.Family.Api.IntegrationTests.csproj -c Release`

## Time Spent
- 1h 55m
