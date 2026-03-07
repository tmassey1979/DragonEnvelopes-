# Issue #204 - Compose Microservice Profile

## Delivered
- Added profile-gated split API services to `docker-compose.yml`:
  - `family-api` (builds `src/DragonEnvelopes.Family.Api/Dockerfile`)
  - `ledger-api` (builds `src/DragonEnvelopes.Ledger.Api/Dockerfile`)
- Added runtime wiring for both services:
  - Postgres/Keycloak dependencies
  - auth and DB environment variables
  - host port mappings
  - `/health/ready` health checks
- Added new `.env` defaults in `.env.example`:
  - `FAMILY_API_PORT=18089`
  - `LEDGER_API_PORT=18090`
- Updated `README.md` with:
  - microservices profile startup command
  - new environment variable documentation
  - Family/Ledger API endpoint and health URL references
- Preserved default monolith compose startup behavior.

## Validation
- `docker compose config`
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
