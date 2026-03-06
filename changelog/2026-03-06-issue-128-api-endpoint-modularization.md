# Issue #128 - API Endpoint Registration Refactor

## Summary
Moved business-domain minimal API route registrations out of `Program.cs` into dedicated endpoint modules and retained `Program.cs` as a composition root.

## What Changed
- Added endpoint modules under `src/DragonEnvelopes.Api/Endpoints`:
  - `SystemAndAuthEndpoints.cs`
  - `FamilyEndpoints.cs`
  - `AutomationEndpoints.cs`
  - `AccountAndTransactionEndpoints.cs`
  - `PlanningAndReportingEndpoints.cs`
- Added shared endpoint helpers:
  - `EndpointMappers.cs` for API contract response mapping.
  - `EndpointAccessGuards.cs` for family membership authorization checks.
- Simplified `src/DragonEnvelopes.Api/Program.cs`:
  - Kept startup/service/middleware/versioning/health setup.
  - Replaced inline route handlers with chained endpoint module registration.
  - Preserved auth and issuer validation helpers.

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`

## Notes
- No API route contract changes were introduced.
- This is a modular-monolith cleanup; microservice decomposition can now proceed incrementally by endpoint module.
