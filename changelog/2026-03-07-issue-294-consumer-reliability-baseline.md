# Issue 294 - Consumer Reliability Baseline (Inbox + Retry + DLQ)

## Summary
Implemented the Financial ledger transaction consumer reliability baseline with deterministic inbox idempotency, retry routing, poison/dead-letter handling, and integration coverage for duplicate delivery and retry exhaustion.

## Delivered
- Added inbox persistence model for consumer idempotency and processing lifecycle:
  - `IntegrationInboxMessage` domain entity.
  - EF config + `integration_inbox_messages` migration.
  - repository contract + implementation (`IIntegrationInboxRepository`, `IntegrationInboxRepository`).
- Added deterministic idempotency strategy:
  - `idempotencyKey = consumerName:sourceService:eventId` (normalized lowercase).
  - duplicate processed/dead-lettered deliveries are acknowledged without re-processing.
- Added Financial consumer processing layer:
  - `LedgerTransactionCreatedMessageProcessor` with envelope/raw payload compatibility parsing.
  - attempt tracking, retry transition, dead-letter transition, and poison-message dead-lettering.
  - pluggable handler interface (`ILedgerTransactionCreatedEventHandler`) with default logging handler.
- Updated Financial RabbitMQ consumer topology and behavior:
  - queue declarations for main/retry/DLQ.
  - retry republish path using retry routing key and TTL queue dead-letter back to main.
  - terminal dead-letter publish path with metadata headers.
  - queue depth snapshot logging for operational observability.
- Extended shared RabbitMQ options/config:
  - retry queue/routing keys, dead-letter queue/routing keys, max retry attempts, retry delay.
- Added integration tests (Financial API test project):
  - duplicate delivery is idempotent (single processing execution).
  - retry exhaustion transitions message to dead-letter state.
- Added runbook:
  - `docs/operations/rabbitmq-consumer-reliability.md` with topology, idempotency contract, retry/DLQ behavior, and replay procedure.
- CI update:
  - Linux split integration test job now runs Financial API integration tests.

## Validation
- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -c Release` (134 passed)
- `dotnet test tests/DragonEnvelopes.Financial.Api.IntegrationTests/DragonEnvelopes.Financial.Api.IntegrationTests.csproj -c Release` (2 passed)
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -c Release` (113 passed)
- `dotnet test tests/DragonEnvelopes.Family.Api.IntegrationTests/DragonEnvelopes.Family.Api.IntegrationTests.csproj -c Release` (11 passed)
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -c Release` (24 passed)

## Time Spent
- 2h 20m
