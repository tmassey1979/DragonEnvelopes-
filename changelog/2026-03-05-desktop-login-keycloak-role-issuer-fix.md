# 2026-03-05 - desktop login reliability after family onboarding

## Summary
- Fixed family onboarding to assign the Keycloak `Parent` realm role to the newly created guardian user.
- Added API JWT token intent validation to allow trusted Keycloak authorized parties (`azp`) for desktop and API clients.
- Added dual-issuer support so docker-internal and localhost-issued Keycloak tokens both validate in local development.
- Updated compose/auth runtime documentation for `Authentication__PublicAuthority`.

## Files Changed
- src/DragonEnvelopes.Api/Program.cs
- src/DragonEnvelopes.Api/Services/IKeycloakProvisioningService.cs
- src/DragonEnvelopes.Api/Services/KeycloakProvisioningService.cs
- src/DragonEnvelopes.Api/appsettings.json
- src/DragonEnvelopes.Api/appsettings.Development.json
- docker-compose.yml
- README.md

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release --no-build`
- `docker compose up -d --build api`
- End-to-end check:\
  - onboard family via `POST /api/v1/families/onboard`\
  - obtain token via Keycloak password grant for `dragonenvelopes-desktop`\
  - call `GET /api/v1/auth/me` and confirm `200 OK`
