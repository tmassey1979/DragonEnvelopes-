# Changelog - 2026-03-06 - Issue #126

## Summary
Implemented onboarding wizard data-entry steps and bootstrap submit integration for starter accounts, envelopes, and budget.

## Changes
- Extended onboarding desktop service:
  - added bootstrap submit call to `/families/{familyId}/onboarding/bootstrap`
  - added `OnboardingBootstrapResultData`
- Added onboarding draft row viewmodels:
  - `OnboardingAccountDraftViewModel`
  - `OnboardingEnvelopeDraftViewModel`
- Extended onboarding wizard viewmodel:
  - account/envelope draft collections
  - add/remove row commands
  - budget month/income fields
  - bootstrap submit command
  - completion update call after successful submit
- Updated onboarding wizard UI template:
  - account rows editor
  - envelope rows editor
  - budget input section
  - submit setup action

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj -nologo -p:OutputPath=bin/TempVerify/`
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`
- Result: all passed. (One transient file-lock test build failure was resolved by immediate rerun.)
