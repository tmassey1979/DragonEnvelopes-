# Issue #219 - API cleanup: remove sample weatherforecast endpoint and stale docs

## Summary
Removed the sample weather endpoint from the versioned API surface and cleaned startup/docs references so system health/version endpoints are the canonical lightweight probes.

## Delivered
- Removed weather endpoint route and record type:
  - `src/DragonEnvelopes.Api/Endpoints/SystemAndAuthEndpoints.cs`
- Removed weather summary wiring from startup/bootstrap:
  - `src/DragonEnvelopes.Api/Bootstrap/ApiBootstrap.Runtime.cs`
  - `src/DragonEnvelopes.Api/Program.cs`
- Updated API HTTP sample:
  - `src/DragonEnvelopes.Api/DragonEnvelopes.Api.http`
- Updated docs:
  - `README.md` (observability verification traffic + versioned endpoint examples)
- Verified no remaining `weatherforecast` references in API/docs.

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj -v minimal`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -v minimal`

All validation commands completed successfully.
