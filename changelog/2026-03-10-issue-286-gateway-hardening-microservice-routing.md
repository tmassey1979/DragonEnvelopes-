# Issue 286 - Gateway Hardening for Microservice-Only Routing

## Summary
Hardened gateway routing to use split domain services only, removed monolith fallback behavior, added downstream-aware readiness probes, and added CI route-level gateway smoke validation.

## Delivered
- Updated gateway routing config:
  - `infrastructure/nginx/api-gateway.conf`
  - removed monolith fallback (`@monolith_fallback`) and fallback error-page behavior
  - default unmapped API routes now return `404`
  - route map continues explicit ownership for Family/Ledger/Financial domains
  - added downstream readiness proxies:
    - `/health/ready/family`
    - `/health/ready/ledger`
    - `/health/ready/financial`
- Updated compose topology:
  - `docker-compose.yml`
  - `api-gateway` is now in `microservices` profile
  - gateway depends on `family-api`, `ledger-api`, `financial-api` (no monolith dependency)
- Added route-level smoke checks:
  - `eng/run-gateway-route-smoke.sh`
  - validates representative Family/Ledger/Financial gateway routes return expected non-404 status families
- Updated CI workflow:
  - `compose-runtime-smoke` and `observability-compose-smoke` now start `api-gateway` explicitly with split services
  - readiness waits include gateway downstream probes (`/health/ready/family|ledger|financial`)
  - added `Run gateway route ownership smoke checks` step
- Added route ownership documentation:
  - `docs/architecture/gateway-route-ownership.md`
- Updated docs references:
  - `docs/README.md`
  - `docs/technical-guide.md`

## Validation
- `bash -n eng/run-gateway-route-smoke.sh`
- `docker compose --profile microservices config`
- `docker compose --profile observability --profile microservices config`
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
- 1h 20m
