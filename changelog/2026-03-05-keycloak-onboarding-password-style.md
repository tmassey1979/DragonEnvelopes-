# 2026-03-05 - keycloak-backed family onboarding and password field style parity

## Summary
- Added a single onboarding API endpoint that creates the guardian identity in Keycloak, then creates the family and parent membership in app data.
- Added compensation logic to delete the Keycloak user if downstream family/member persistence fails.
- Added onboarding request validation for family name, guardian name, email, and password.
- Updated desktop create-family flow to call the single onboarding endpoint.
- Styled `PasswordBox` to match textbox visual treatment in WPF auth flows.
- Updated docs and compose/env settings for Keycloak admin token configuration.

## Files Changed
- src/DragonEnvelopes.Contracts/Families/CompleteFamilyOnboardingRequest.cs
- src/DragonEnvelopes.Api/CrossCutting/Validation/Validators/RequestValidators.cs
- src/DragonEnvelopes.Api/Services/KeycloakAdminOptions.cs
- src/DragonEnvelopes.Api/Services/IKeycloakProvisioningService.cs
- src/DragonEnvelopes.Api/Services/KeycloakProvisioningService.cs
- src/DragonEnvelopes.Api/Program.cs
- src/DragonEnvelopes.Api/appsettings.json
- src/DragonEnvelopes.Api/appsettings.Development.json
- client/DragonEnvelopes.Desktop/Services/FamilyAccountService.cs
- client/DragonEnvelopes.Desktop/Resources/Themes/Controls.xaml
- docker-compose.yml
- .env.example
- README.md

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release --no-build`
- `docker compose up -d --build api`
- `Invoke-RestMethod -Method Post http://localhost:18088/api/v1/families/onboard ...` (returned `201 Created`)
