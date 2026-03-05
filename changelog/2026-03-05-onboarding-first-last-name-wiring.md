# 2026-03-05 - onboarding first and last name wiring across UI and API

## Summary
- Updated family onboarding contract to capture guardian first name and last name explicitly.
- Updated Create Family WPF UI to collect first and last name in separate fields.
- Updated desktop onboarding service request mapping to send first/last names to API.
- Updated API onboarding validation and Keycloak provisioning to set `firstName`/`lastName` directly.
- Updated API family-member bootstrap naming to persist combined guardian display name.

## Files Changed
- client/DragonEnvelopes.Desktop/Views/CreateFamilyAccountWindow.xaml
- client/DragonEnvelopes.Desktop/Views/CreateFamilyAccountWindow.xaml.cs
- client/DragonEnvelopes.Desktop/Services/CreateFamilyAccountRequest.cs
- client/DragonEnvelopes.Desktop/Services/FamilyAccountService.cs
- src/DragonEnvelopes.Contracts/Families/CompleteFamilyOnboardingRequest.cs
- src/DragonEnvelopes.Api/CrossCutting/Validation/Validators/RequestValidators.cs
- src/DragonEnvelopes.Api/Program.cs
- src/DragonEnvelopes.Api/Services/IKeycloakProvisioningService.cs
- src/DragonEnvelopes.Api/Services/KeycloakProvisioningService.cs

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release --no-build`
- `docker compose up -d --build api`
- End-to-end verification: onboard request with first/last name -> Keycloak user lookup confirms both fields are populated
