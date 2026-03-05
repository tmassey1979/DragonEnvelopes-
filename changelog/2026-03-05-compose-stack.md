# 2026-03-05 - Docker Compose Platform Stack

## Summary

Implemented the local Docker platform stack for `DragonEnvelopes` with PostgreSQL, pgAdmin, Keycloak, and API services in one compose network.

## Completed Stories

- #41 Create docker-compose postgres and pgAdmin services
- #42 Add keycloak and api services to docker-compose network

## Key Changes

- Added [docker-compose.yml](../docker-compose.yml) with services:
  - `postgres:16`
  - `dpage/pgadmin4`
  - `quay.io/keycloak/keycloak`
  - API build from `src/DragonEnvelopes.Api/Dockerfile`
- Added persistent named volumes:
  - `postgres_data`
  - `pgadmin_data`
- Added database bootstrap script:
  - [infrastructure/postgres/init/01-create-databases.sh](../infrastructure/postgres/init/01-create-databases.sh)
  - Creates `dragonenvelopes_app` and `keycloak` databases.
- Added pgAdmin auto-registration assets:
  - [infrastructure/pgadmin/servers.json](../infrastructure/pgadmin/servers.json)
  - [infrastructure/pgadmin/pgpass](../infrastructure/pgadmin/pgpass)
- Added environment template:
  - [.env.example](../.env.example)
- Updated [README.md](../README.md) with compose startup and endpoint docs.

## Validation

- `docker compose config`
- `docker compose up -d --build`
- `docker compose ps` showed all four services running.
- `docker exec dragonenvelopes-postgres psql -U postgres -tAc "SELECT datname FROM pg_database WHERE datname IN ('dragonenvelopes_app','keycloak') ORDER BY datname;"` returned both databases.
- `Invoke-WebRequest http://localhost:18088/weatherforecast` returned `200`.
- `Invoke-WebRequest http://localhost:18080` returned `200` after Keycloak warm-up.

## Notes

- Default host ports were moved to avoid common local conflicts:
  - Postgres `5433`
  - Keycloak `18080`
  - API `18088`
  - pgAdmin `5050`

