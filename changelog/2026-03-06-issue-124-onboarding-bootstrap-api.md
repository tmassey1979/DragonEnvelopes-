# Changelog - 2026-03-06 - Issue #124

## Summary
Implemented onboarding bootstrap API for atomic starter creation of accounts, envelopes, and optional budget.

## Changes
- Added onboarding bootstrap contracts:
  - `OnboardingBootstrapRequest`
  - `OnboardingBootstrapAccountRequest`
  - `OnboardingBootstrapEnvelopeRequest`
  - `OnboardingBootstrapBudgetRequest`
  - `OnboardingBootstrapResponse`
- Added application layer:
  - `IOnboardingBootstrapService`, `OnboardingBootstrapService`
  - `IOnboardingBootstrapRepository`
  - DTO `OnboardingBootstrapDetails`
- Added infrastructure repository:
  - `OnboardingBootstrapRepository`
  - atomic `SaveBootstrapAsync` persistence path
- Added API endpoint:
  - `POST /api/v1/families/{familyId}/onboarding/bootstrap`
- Added request validators for bootstrap payload and nested account/envelope/budget rows.
- Added integration tests:
  - own-family bootstrap success
  - cross-family bootstrap forbidden

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`
- Result: build succeeded; integration tests passed (17/17).
- Note: one transient MSBuild copy-retry warning occurred during test build due to temporary file lock, but tests and build completed successfully.
