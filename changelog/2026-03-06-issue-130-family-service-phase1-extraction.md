# Issue #130 - Family/Identity Service Extraction (Phase 1)

## Summary
Introduced a new deployable service host `DragonEnvelopes.Family.Api` as the first microservice extraction step for family/identity endpoints.

## What Was Added
- New project: `src/DragonEnvelopes.Family.Api`
- Added to solution: `DragonEnvelopes.sln`
- Family service endpoint surface via copied family endpoint module:
  - family create/get/member routes
  - family invite create/list/cancel/accept
  - onboarding profile and bootstrap routes
- Service startup includes:
  - JWT auth + family role policies
  - FluentValidation endpoint filter
  - Swagger/OpenAPI + bearer security definition
  - health endpoints (`/health/live`, `/health/ready`)
  - EF migrations at startup
- Service-specific appsettings with dedicated default DB target:
  - `Database=dragonenvelopes_family`
- Dockerfile for family service image build/publish.

## Validation
- `dotnet build src/DragonEnvelopes.Family.Api/DragonEnvelopes.Family.Api.csproj -nologo`
- `dotnet build DragonEnvelopes.sln -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`

## Phase 1 Notes
- This extraction is deployable and route-complete for family APIs, but still reuses shared Application/Infrastructure assemblies.
- Follow-up hardening for strict domain-isolated infrastructure and routing cutover remains for next phase tasks.
