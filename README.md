# DragonEnvelopes

Initial implementation uses .NET 8 with a Clean Architecture layout.

## Solution

- `DragonEnvelopes.sln`
- `src/DragonEnvelopes.Domain`
- `src/DragonEnvelopes.Application`
- `src/DragonEnvelopes.Infrastructure`
- `src/DragonEnvelopes.Api`
- `src/DragonEnvelopes.Contracts`
- `client/DragonEnvelopes.Desktop`
- `tests/DragonEnvelopes.Domain.Tests`
- `tests/DragonEnvelopes.Application.Tests`

## Architecture Dependencies

- Domain: no project references.
- Application: references Domain.
- Infrastructure: references Application and Domain.
- API: references Application, Infrastructure, and Contracts.
- Desktop: references Contracts only.
- Tests: reference only the layer they validate.

## Local Commands

```powershell
dotnet restore
dotnet build DragonEnvelopes.sln
dotnet test DragonEnvelopes.sln
```

## Docker Services

Create a local `.env` from `.env.example` and start the infrastructure stack:

```powershell
Copy-Item .env.example .env
docker compose up -d --build
docker compose ps
```

Default local endpoints:

- API: `http://localhost:18088`
- Keycloak: `http://localhost:18080`
- pgAdmin: `http://localhost:5050`
- Postgres: `localhost:5433`

## Keycloak Bootstrap

- Compose imports realm config from `infrastructure/keycloak/import/dragonenvelopes-realm.json`.
- Open Keycloak admin at `http://localhost:18080`.
- Admin credentials come from `.env`:
  - `KEYCLOAK_BOOTSTRAP_ADMIN_USERNAME`
  - `KEYCLOAK_BOOTSTRAP_ADMIN_PASSWORD`
- Imported realm: `dragonenvelopes`
- Imported roles:
  - `Parent`
  - `Adult`
  - `Teen`
  - `Child`
- Imported clients:
  - `dragonenvelopes-api`
  - `dragonenvelopes-desktop`



