# Issue 287 - Microservice Delivery Pipeline Expansion

## Summary
Expanded CI/CD coverage for split API services with parallel per-service build/test execution, GHCR image publish automation for all APIs, and service-level promotion/rollback runbook guidance.

## Delivered
- Updated CI workflow (`.github/workflows/ci.yml`):
  - `linux-service-build` now runs per-service restore/build in parallel (matrix for API, Family, Ledger, Financial).
  - Added `split-integration-tests` matrix job to run Family/Ledger/Financial integration suites in parallel.
  - Added `docker-publish-ghcr` job (push-to-main only) to publish all API service images to GHCR:
    - `dragonenvelopes-api`
    - `dragonenvelopes-family-api`
    - `dragonenvelopes-ledger-api`
    - `dragonenvelopes-financial-api`
  - Publish job runs only after build/test/smoke gates pass.
- Added service delivery operations runbook:
  - `docs/operations/service-delivery-pipeline.md`
  - Includes pipeline stage map, immutable tag promotion guidance, and per-service rollback procedure.
- Updated docs references:
  - `docs/README.md`
  - `docs/technical-guide.md`

## Validation
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
- Compose profile validation:
  - `docker compose --profile microservices config`
  - `docker compose --profile observability --profile microservices config`

## Time Spent
- 1h 05m
