# Changelog - 2026-03-06 - Issue #123

## Summary
Implemented family-scoped onboarding profile/status APIs with persistence, migration, and integration coverage.

## Changes
- Added domain entity:
  - `OnboardingProfile`
- Added contracts:
  - `OnboardingProfileResponse`
  - `UpdateOnboardingProfileRequest`
- Added application layer:
  - `IOnboardingProfileService`, `OnboardingProfileService`
  - `IOnboardingProfileRepository`
  - DTO `OnboardingProfileDetails`
- Added infrastructure layer:
  - `OnboardingProfileRepository`
  - EF config `OnboardingProfileConfiguration`
  - DbSet in `DragonEnvelopesDbContext`
  - migration `AddOnboardingProfiles`
- Added API endpoints:
  - `GET /api/v1/families/{familyId}/onboarding`
  - `PUT /api/v1/families/{familyId}/onboarding`
- Added integration tests:
  - user can get/update own family onboarding profile
  - cross-family update returns forbidden

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`
- Result: build succeeded; integration tests passed (15/15).
