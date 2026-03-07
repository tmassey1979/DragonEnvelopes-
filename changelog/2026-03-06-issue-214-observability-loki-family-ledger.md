# Issue #214 - Observability: enable Grafana Loki logging for Family and Ledger APIs

## Summary
Implemented Loki sink parity for split microservices so Family API and Ledger API can ship structured logs directly to Grafana Loki when observability is enabled.

## Delivered
- Added `Serilog.Sinks.Grafana.Loki` package references:
  - `src/DragonEnvelopes.Family.Api/DragonEnvelopes.Family.Api.csproj`
  - `src/DragonEnvelopes.Ledger.Api/DragonEnvelopes.Ledger.Api.csproj`
- Added optional Loki sink wiring in API bootstrap:
  - `src/DragonEnvelopes.Family.Api/Bootstrap/FamilyApiBootstrap.Observability.cs`
  - `src/DragonEnvelopes.Ledger.Api/Bootstrap/LedgerApiBootstrap.Observability.cs`
- Added config support in Family/Ledger settings:
  - `Observability:EnableLokiSink`
  - `Observability:LokiUrl`
  - Added sink discovery in Serilog `Using` arrays.
- Added compose env wiring for split services:
  - `docker-compose.yml`
  - `Observability__EnableLokiSink`
  - `Observability__LokiUrl`
- Updated observability docs and dashboard queries for all three services:
  - `README.md`
  - `infrastructure/observability/grafana/dashboards/dragonenvelopes-api-logs.json`

## Validation
- `dotnet build src/DragonEnvelopes.Family.Api/DragonEnvelopes.Family.Api.csproj -v minimal`
- `dotnet build src/DragonEnvelopes.Ledger.Api/DragonEnvelopes.Ledger.Api.csproj -v minimal`
- `docker compose --profile observability --profile microservices config`

All validation commands completed successfully.
