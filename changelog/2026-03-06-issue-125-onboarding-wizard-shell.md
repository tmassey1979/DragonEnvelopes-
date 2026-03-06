# Changelog - 2026-03-06 - Issue #125

## Summary
Added desktop onboarding wizard shell with step navigation, progress indicator, and resume behavior using onboarding profile APIs.

## Changes
- Added onboarding desktop data service:
  - `IOnboardingDataService`
  - `OnboardingDataService`
  - `OnboardingProfileData`
- Added onboarding wizard viewmodel:
  - `OnboardingWizardViewModel`
  - step navigation (`Back`, `Next`, `Mark Complete`, `Cancel`)
  - progress percent based on onboarding milestones
  - resume behavior from API status (`GetProfileAsync`)
- Added route:
  - `/onboarding` (parent-role gated)
- Added onboarding wizard UI template in shell resources.

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj -nologo -p:OutputPath=bin/TempVerify/`
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`
- Result: all passed.
