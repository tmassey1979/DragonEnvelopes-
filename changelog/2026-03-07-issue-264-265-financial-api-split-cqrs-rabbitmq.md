# 2026-03-07 - Issues #264 and #265

## Summary
- Added `DragonEnvelopes.Financial.Api` as a dedicated split service and wired solution/compose/CI/CD for build, runtime smoke, and GHCR publish.
- Routed desktop financial integrations/onboarding traffic to a dedicated financial API client (`DRAGONENVELOPES_FINANCIAL_API_BASE_URL`) while preserving family/ledger split routing.
- Extended desktop runtime settings checks to include financial API health messaging.
- Implemented CQRS baseline for ledger transactions:
  - Added command/query abstractions and in-process command/query buses.
  - Added `CreateTransactionCommand` and `ListTransactionsByAccountQuery` handlers.
  - Updated ledger transaction create/list endpoints to dispatch via CQRS buses.
- Added RabbitMQ integration event pipeline:
  - Publisher abstraction + RabbitMQ publisher + no-op fallback.
  - Published `ledger.transaction.created.v1` event on transaction create command success.
  - Added financial API hosted consumer for `ledger.transaction.created.v1` with ack/nack semantics and retry-on-start behavior.
- Added RabbitMQ container/service wiring in compose and environment/config defaults.

## Validation
- `dotnet build DragonEnvelopes.sln -c Release --nologo`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -c Release --nologo`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -c Release --nologo`
- `docker compose --profile microservices up -d --build`
- Health checks:
  - `http://localhost:18088/health/ready` -> `200`
  - `http://localhost:18089/health/ready` -> `200`
  - `http://localhost:18090/health/ready` -> `200`
  - `http://localhost:18091/health/ready` -> `200`
- RabbitMQ runtime checks:
  - Exchange exists: `dragonenvelopes.events` (`topic`)
  - Queue exists: `dragonenvelopes.financial.ledger-transaction-created` with active consumer

## Notes
- RabbitMQ publisher logs and continues when publish fails to prevent write-path hard outages.
- Financial consumer retries startup if RabbitMQ is temporarily unavailable during service boot.
