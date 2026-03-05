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
- `KEYCLOAK_ADMIN_REALM`: Realm used for Keycloak admin token flow (typically `master`).
- `KEYCLOAK_ADMIN_CLIENT_ID`: Admin client id for token acquisition (typically `admin-cli`).
- `OBSERVABILITY_ENABLE_LOKI_SINK`: Enables direct API -> Loki sink (`true` or `false`).
- `OBSERVABILITY_LOKI_URL`: Loki base URL used by API sink.
- `LOKI_PORT`: Host port mapped to Loki container `3100` (observability profile).
- `GRAFANA_PORT`: Host port mapped to Grafana container `3000` (observability profile).
- `GRAFANA_ADMIN_USER`: Grafana admin username (observability profile).
- `GRAFANA_ADMIN_PASSWORD`: Grafana admin password (observability profile).

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

### API Container Runtime Notes

- API image uses a multi-stage Docker build (`restore` -> `publish` -> `runtime`).
- Runtime container runs as non-root user `dragonenvelopes` (UID/GID `10001` by default).
- Runtime port inside container: `8080` (`ASPNETCORE_HTTP_PORTS=8080`).
- Required API runtime environment variables:
  - `ConnectionStrings__Default`
  - `Authentication__Authority`
  - `Authentication__PublicAuthority`
  - `Authentication__Audience`

API health endpoints:

- Liveness: `http://localhost:18088/health/live`
- Readiness: `http://localhost:18088/health/ready`

### Observability Profile (Grafana + Loki + Promtail)

Start with API log shipping enabled:

```powershell
$env:OBSERVABILITY_ENABLE_LOKI_SINK='true'
docker compose --profile observability up -d
```

Default observability endpoints:

- Loki: `http://localhost:3100`
- Grafana: `http://localhost:3000`
- Promtail metrics: `http://localhost:9080/metrics` (inside container network)

Grafana defaults (from `.env`):

- Username: `admin`
- Password: `admin`

Provisioned Grafana assets:

- Loki datasource: `DragonEnvelopes Loki`
- Dashboard: `DragonEnvelopes API Logs`

Saved log query patterns:

- API error rate (5xx over 5 minutes):
  - `sum(count_over_time({application="dragonenvelopes-api"} |= "HTTP" | json | StatusCode=~"5.." [5m]))`
- API trace + exception details:
  - `{application="dragonenvelopes-api"} | json | line_format "{{.level}} {{.CorrelationId}} {{.RequestPath}} {{.StatusCode}} {{.ExceptionType}} {{.message}}"`

Verification flow:

1. Start stack with observability profile.
2. Generate API traffic:
   - `Invoke-WebRequest http://localhost:18088/health/live`
   - `Invoke-WebRequest http://localhost:18088/api/v1/weatherforecast`
3. Open Grafana Explore and run:
   - `{application="dragonenvelopes-api"}`
4. Confirm logs include `CorrelationId`, `RequestPath`, and `StatusCode`.

Troubleshooting:

- If no logs appear, verify API sink flag:
  - `OBSERVABILITY_ENABLE_LOKI_SINK=true`
- Check Loki ingestion health:
  - `Invoke-WebRequest http://localhost:3100/ready`
- Validate promtail targets:
  - `docker compose logs promtail`
- Confirm API container can resolve Loki service:
  - `docker compose exec api printenv Observability__LokiUrl`

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
  - `GET /api/v1/auth/me` (requires `AnyFamilyMemberPolicy`)
  - `GET /api/v1/auth/parent-only` (requires `ParentPolicy`)

## Desktop OIDC Sign-In (WPF)

- Desktop client uses OIDC Authorization Code + PKCE with system browser callback loopback.
- Default desktop auth settings:
  - Authority: `http://localhost:18080/realms/dragonenvelopes`
  - Client Id: `dragonenvelopes-desktop`
  - Redirect URI: `http://127.0.0.1:7890/callback/`
  - Scope: `openid profile email offline_access`
- Optional environment overrides:
  - `DRAGONENVELOPES_AUTH_AUTHORITY`
  - `DRAGONENVELOPES_AUTH_CLIENT_ID`
  - `DRAGONENVELOPES_AUTH_REDIRECT_URI`
  - `DRAGONENVELOPES_AUTH_SCOPE`
- Session persistence:
  - Token session is encrypted at rest using Windows DPAPI (`DataProtectionScope.CurrentUser`).
  - Session file location: `%LOCALAPPDATA%\\DragonEnvelopes\\session.dat`.

## Desktop Authenticated API Client

- WPF desktop client uses a centralized authenticated HTTP pipeline.
- Bearer tokens are attached in a delegating handler (`AuthenticatedApiHttpMessageHandler`).
- Handler behavior:
  - Pre-refreshes/renews token when nearing expiry (using refresh token when available).
  - Retries idempotent requests once on `401` after forced refresh.
  - Leaves non-idempotent requests as non-retried for safety.
- Optional API base URL override:
  - `DRAGONENVELOPES_API_BASE_URL` (default `http://localhost:18088/api/v1/`)

## Desktop Installer (MSI)

- Installer source is in `installer/DragonEnvelopes.Desktop.Installer` (WiX Toolset v4 SDK project).
- Build flow:
  1. Publish desktop app payload.
  2. Build WiX MSI using publish output as installer content.

Local build commands:

```powershell
dotnet publish client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:SatelliteResourceLanguages=en --output artifacts/desktop/publish
dotnet build installer/DragonEnvelopes.Desktop.Installer/DragonEnvelopes.Desktop.Installer.wixproj --configuration Release -p:ProductVersion=1.0.0 -p:PublishDir="$PWD\artifacts\desktop\publish"
```

Expected MSI output:

- `installer/DragonEnvelopes.Desktop.Installer/bin/x64/Release/DragonEnvelopes.Desktop.Setup.msi`

## API Versioning and Error Contract

- Versioning mode: URL segment.
  - Route prefix pattern: `/api/v{version}`
  - Current version: `v1`
- Example versioned endpoints:
  - `GET /api/v1/weatherforecast`
  - `GET /api/v1/auth/me`
  - `POST /api/v1/families`
  - `POST /api/v1/families/onboard`
  - `GET /api/v1/families/{familyId}`
  - `POST /api/v1/families/{familyId}/members`
  - `GET /api/v1/families/{familyId}/members`
  - `POST /api/v1/accounts`
  - `GET /api/v1/accounts?familyId={familyId}`
  - `POST /api/v1/transactions`
  - `GET /api/v1/transactions?accountId={accountId}`
  - `POST /api/v1/envelopes`
  - `GET /api/v1/envelopes?familyId={familyId}`
  - `GET /api/v1/envelopes/{envelopeId}`
  - `PUT /api/v1/envelopes/{envelopeId}`
  - `POST /api/v1/envelopes/{envelopeId}/archive`
- Bootstrap access:
  - `POST /api/v1/families`, `POST /api/v1/families/onboard`, and `POST /api/v1/families/{familyId}/members` allow anonymous requests to support first-time desktop onboarding.
- OpenAPI/Swagger includes:
  - Bearer auth security scheme (`Authorization: Bearer <token>`)
  - Auth operation annotations for expected `401` and `403` responses
- Error contract format:
  - Problem responses follow RFC7807 (`application/problem+json`)
  - Standard fields: `type`, `title`, `status`, `detail`, `instance`
  - API adds `traceId` for correlation
  - Validation errors return `HttpValidationProblemDetails` with an `errors` dictionary



