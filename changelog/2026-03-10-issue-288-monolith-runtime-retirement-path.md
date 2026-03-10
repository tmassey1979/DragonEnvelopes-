# Issue 288 - Monolith Runtime Retirement Path

## Summary
Moved monolith API runtime behind an explicit legacy profile, kept gateway/client traffic aligned to split services, and added decommission + rollback documentation for controlled retirement.

## Delivered
- Updated compose runtime defaults:
  - `docker-compose.yml`
  - `api` (`DragonEnvelopes.Api`) moved to `legacy-monolith` profile only
  - default compose service set no longer includes monolith API runtime
- Gateway and client traffic dependency posture:
  - gateway remains microservices-profile route surface (`:18088`)
  - desktop default base URL remains gateway (`DRAGONENVELOPES_API_BASE_URL` default)
  - docs explicitly mark monolith as legacy/rollback-only path
- Added decommission checklist and rollback strategy:
  - `docs/operations/monolith-decommission-checklist.md`
- Updated documentation references and environment notes:
  - `docs/technical-guide.md`
  - `docs/README.md`
  - `.env.example` monolith variable annotated as legacy profile only

## Validation
- Compose service set verification:
  - `docker compose config --services`
    - default includes infra only (no monolith API runtime)
  - `docker compose --profile microservices config --services`
    - includes `api-gateway`, `family-api`, `ledger-api`, `financial-api`
- `bash -n eng/run-gateway-route-smoke.sh`
- `dotnet test DragonEnvelopes.sln -c Release`
  - `DragonEnvelopes.Domain.Tests`: 6 passed
  - `DragonEnvelopes.Application.Tests`: 143 passed
  - `DragonEnvelopes.Desktop.Tests`: 102 passed
  - `DragonEnvelopes.Financial.Api.IntegrationTests`: 6 passed
  - `DragonEnvelopes.Family.Api.IntegrationTests`: 13 passed
  - `DragonEnvelopes.Ledger.Api.IntegrationTests`: 31 passed
  - `DragonEnvelopes.Api.IntegrationTests`: 113 passed
- `./eng/verify-contract-drift.ps1 -RepoRoot .`
- `./eng/verify-event-contract-compatibility.ps1 -RepoRoot .`

## Time Spent
- 55m
