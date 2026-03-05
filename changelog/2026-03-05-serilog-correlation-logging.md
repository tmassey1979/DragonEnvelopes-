# 2026-03-05 - Serilog Structured Logging with Correlation IDs

## Summary

Configured Serilog as the API logging provider, added correlation ID propagation middleware, and enabled structured request logging enrichment.

## Completed Story

- #47 Configure Serilog structured logging with correlation IDs

## Key Changes

- Added Serilog packages to API project:
  - `src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj`
- Configured Serilog host integration in startup:
  - Uses appsettings-driven configuration
  - Enriches from log context and sets `Application` property
  - Enables request logging with correlation/request host/client IP enrichment
  - `src/DragonEnvelopes.Api/Program.cs`
- Added correlation middleware:
  - Reads or creates `X-Correlation-ID`
  - Sets `HttpContext.TraceIdentifier`
  - Pushes `CorrelationId` into Serilog `LogContext`
  - Echoes correlation ID on response headers
  - `src/DragonEnvelopes.Api/CrossCutting/Logging/CorrelationIdMiddleware.cs`
- Added Serilog configuration:
  - Console sink output template includes correlation ID
  - Minimum level overrides for framework/system namespaces
  - Dev profile logging level overrides
  - `src/DragonEnvelopes.Api/appsettings.json`
  - `src/DragonEnvelopes.Api/appsettings.Development.json`

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
