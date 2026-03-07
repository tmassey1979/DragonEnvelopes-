# Issue 290 - Shared Event Envelope Contracts

## Summary
Implemented shared event envelope contracts and compatibility helpers, and integrated publish/consume paths with envelope-aware behavior.

## Delivered
- Added shared integration event contracts in `DragonEnvelopes.Contracts/IntegrationEvents`:
  - `IntegrationEventEnvelope<TPayload>`
  - `IntegrationEventEnvelopeFactory`
  - `IntegrationEventEnvelopeJson`
  - `IntegrationEventEnvelopeValidator`
- Updated RabbitMQ publisher to emit envelope-wrapped events with required metadata:
  - `eventId`, `eventName`, `schemaVersion`, `occurredAtUtc`, `publishedAtUtc`, `sourceService`, `correlationId`, optional `familyId`.
- Added `SourceService` option to `RabbitMqMessagingOptions`.
- Updated Financial consumer to:
  - parse envelope-first,
  - validate required envelope metadata,
  - enforce supported major schema version,
  - retain backward-compatible raw payload fallback.
- Added contract tests for envelope serialization/validation behavior.

## Validation
- `dotnet build src/DragonEnvelopes.Infrastructure/DragonEnvelopes.Infrastructure.csproj -c Release`
- `dotnet build src/DragonEnvelopes.Financial.Api/DragonEnvelopes.Financial.Api.csproj -c Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -c Release`

## Time Spent
- 1h 10m
