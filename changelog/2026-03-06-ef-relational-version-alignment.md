# 2026-03-06 - EF Relational Version Alignment for Integration Tests

## Summary
Resolved EF Core relational assembly version conflicts in integration test build graph.

## Changes
- Added explicit `Microsoft.EntityFrameworkCore.Relational` package reference (`8.0.24`) to:
  - `src/DragonEnvelopes.Infrastructure/DragonEnvelopes.Infrastructure.csproj`
  - `tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`

## Validation
- `dotnet build tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj` (pass, 0 warnings)
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj` (pass)

## Notes
- Full solution build currently hits a desktop output lock if app is running; integration test graph verification confirms the previous EF relational conflict warning is removed.
