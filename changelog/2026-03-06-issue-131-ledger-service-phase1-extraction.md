# Issue #131 - Ledger Service Extraction (Phase 1)

## Summary
Introduced a new deployable service host `DragonEnvelopes.Ledger.Api` for the ledger domain API surface.

## What Was Added
- New project: `src/DragonEnvelopes.Ledger.Api`
- Added to solution: `DragonEnvelopes.sln`
- Ledger endpoint surface in standalone host:
  - automation rules endpoints
  - accounts endpoints
  - transactions endpoints
  - import preview/commit endpoints
- Service startup includes:
  - JWT auth + family role policies
  - FluentValidation endpoint filter
  - Swagger/OpenAPI + bearer security definition
  - health endpoints (`/health/live`, `/health/ready`)
  - EF migrations at startup
- Service-specific appsettings with dedicated default DB target:
  - `Database=dragonenvelopes_ledger`
- Dockerfile for ledger service image build/publish.

## Validation
- `dotnet build DragonEnvelopes.sln -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`

## Phase 1 Notes
- Extraction is deployable with route-complete ledger endpoints, while still reusing shared Application/Infrastructure assemblies.
- Follow-up hardening for stricter service-isolated infrastructure and route cutover orchestration can proceed in next platform phase.
