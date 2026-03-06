# 2026-03-06 - Issue #172 Onboarding family profile setup step

## Summary
Added a dedicated onboarding family-profile step that captures and persists `name`, `currency`, and `time zone`, and only marks the step complete after a successful API save.

## Backend changes
- Extended `Family` domain/persistence profile data:
  - `CurrencyCode`
  - `TimeZoneId`
  - `UpdatedAt`
- Added family profile contracts:
  - `UpdateFamilyProfileRequest`
  - `FamilyProfileResponse`
- Added family profile service/repository support:
  - `IFamilyService.GetProfileAsync(...)`
  - `IFamilyService.UpdateProfileAsync(...)`
  - `IFamilyRepository.GetFamilyByIdForUpdateAsync(...)`
  - `IFamilyRepository.SaveChangesAsync(...)`
- Added protected endpoints in both API hosts:
  - `GET /api/v1/families/{familyId}/profile`
  - `PUT /api/v1/families/{familyId}/profile`
- Added validation for family profile update request payload.
- Added migration:
  - `20260306221337_AddFamilyProfileDefaults`
  - adds `families.CurrencyCode`, `families.TimeZoneId`, `families.UpdatedAt`
  - migration defaults: `USD`, `America/Chicago`, `CURRENT_TIMESTAMP`

## Desktop onboarding changes
- Added `FamilyProfileData` and onboarding data-service methods:
  - `GetFamilyProfileAsync()`
  - `UpdateFamilyProfileAsync(...)`
- Updated onboarding wizard model:
  - inserted new first step: `Family Profile`
  - added form fields: family name, currency, time zone
  - added `SaveFamilyProfileCommand`
  - progression for step 0 now depends on successful profile save API response
  - deterministic defaults in UI (`USD`, `America/Chicago`)
- Updated onboarding template UI with a family profile panel and save action.

## Tests
- Updated desktop onboarding tests for new step indexing/progress.
- Added desktop test coverage for family profile save/advance behavior.
- Added API integration tests:
  - user can get/update own family profile
  - user cannot update another family profile
- Added application service coverage for family profile update behavior.

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test DragonEnvelopes.sln`
