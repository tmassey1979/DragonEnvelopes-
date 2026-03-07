# Issue #210 - CI Docker Smoke Builds

## Delivered
- Added `docker-smoke-build` matrix job to `.github/workflows/ci.yml` to build (no push):
  - `src/DragonEnvelopes.Api/Dockerfile`
  - `src/DragonEnvelopes.Family.Api/Dockerfile`
  - `src/DragonEnvelopes.Ledger.Api/Dockerfile`
- Preserved existing `build-test` CI job behavior.
- Fixed Docker restore-stage project reference drift discovered during validation:
  - Added `DragonEnvelopes.ProviderClients.csproj` copy to restore stage in all three service Dockerfiles.
- Fixed Linux/container compile issue in family onboarding endpoint modularization:
  - Added `using DragonEnvelopes.Contracts.Families;` in `FamilyEndpoints.Onboarding.cs`.

## Validation
- Local docker smoke builds (all pass):
  - `docker build --file src/DragonEnvelopes.Api/Dockerfile --tag dragonenvelopes-ci-api:local .`
  - `docker build --file src/DragonEnvelopes.Family.Api/Dockerfile --tag dragonenvelopes-ci-family-api:local .`
  - `docker build --file src/DragonEnvelopes.Ledger.Api/Dockerfile --tag dragonenvelopes-ci-ledger-api:local .`
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
