# 2026-03-05 - API Health Endpoints and Container Readiness

## Summary

Implemented API liveness/readiness endpoints and wired container health checks to readiness so compose startup can validate service dependency status.

## Completed Story

- #52 Implement health live/ready endpoints with database readiness checks

## Key Changes

- Added PostgreSQL health check package in API:
  - `AspNetCore.HealthChecks.NpgSql`
- Updated API startup in [src/DragonEnvelopes.Api/Program.cs](../src/DragonEnvelopes.Api/Program.cs):
  - `/health/live` endpoint (lightweight self-check)
  - `/health/ready` endpoint (Postgres connectivity readiness)
- Added default API connection string config in:
  - [src/DragonEnvelopes.Api/appsettings.json](../src/DragonEnvelopes.Api/appsettings.json)
  - [src/DragonEnvelopes.Api/appsettings.Development.json](../src/DragonEnvelopes.Api/appsettings.Development.json)
- Added `curl` to API runtime image in [src/DragonEnvelopes.Api/Dockerfile](../src/DragonEnvelopes.Api/Dockerfile) for container health probe support.
- Added API service healthcheck in [docker-compose.yml](../docker-compose.yml) targeting `/health/ready`.
- Updated [README.md](../README.md) with health endpoint URLs.

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
- `docker compose up -d --build`
- `docker compose ps` confirmed API reports `healthy`
- `Invoke-WebRequest http://localhost:18088/health/live` returned `200`
- `Invoke-WebRequest http://localhost:18088/health/ready` returned `200`
- `docker compose down`

