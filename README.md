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

### Environment Variables

`docker-compose.yml` and API startup rely on these variables from `.env`:

- `POSTGRES_USER`: Postgres username.
- `POSTGRES_PASSWORD`: Postgres password.
- `POSTGRES_DB`: Initial bootstrap database.
- `APP_DB_NAME`: Application database name.
- `KEYCLOAK_DB_NAME`: Keycloak database name.
- `POSTGRES_PORT`: Host port mapped to Postgres container `5432`.
- `PGADMIN_PORT`: Host port mapped to pgAdmin container `80`.
- `KEYCLOAK_PORT`: Host port mapped to Keycloak container `8080`.
- `API_PORT`: Host port mapped to API container `8080`.
- `PGADMIN_DEFAULT_EMAIL`: pgAdmin login email.
- `PGADMIN_DEFAULT_PASSWORD`: pgAdmin login password.
- `KEYCLOAK_BOOTSTRAP_ADMIN_USERNAME`: Keycloak bootstrap admin username.
- `KEYCLOAK_BOOTSTRAP_ADMIN_PASSWORD`: Keycloak bootstrap admin password.
- `KEYCLOAK_REALM`: Realm used by API auth config.
- `KEYCLOAK_CLIENT_ID`: Client/audience used by API auth config.

### Secret Handling Guidance

- Do not commit real credentials; `.env` is ignored by git.
- Commit only `.env.example` with non-secret local defaults.
- For production, use your platform secret store (for example GitHub Actions Secrets, Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault).
- Rotate credentials after sharing dev environments or exporting compose volumes.

Default local endpoints:

- API: `http://localhost:18088`
- Keycloak: `http://localhost:18080`
- pgAdmin: `http://localhost:5050`
- Postgres: `localhost:5433`

API health endpoints:

- Liveness: `http://localhost:18088/health/live`
- Readiness: `http://localhost:18088/health/ready`

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

## API Authentication and Authorization

- API auth settings are bound from configuration/environment:
  - `Authentication__Authority`
  - `Authentication__Audience`
- JWT bearer validation uses Keycloak issuer metadata from `Authority` and validates `Audience`.
- Keycloak realm/client role claims are mapped to API role claims.
- Configured authorization policies:
  - `ParentPolicy`
  - `AdultPolicy`
  - `TeenPolicy`
  - `ChildPolicy`
  - `ParentOrAdultPolicy`
  - `TeenOrAbovePolicy`
  - `AnyFamilyMemberPolicy`
- Auth probe endpoints:
  - `GET /auth/me` (requires `AnyFamilyMemberPolicy`)
  - `GET /auth/parent-only` (requires `ParentPolicy`)



