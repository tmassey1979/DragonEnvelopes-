# 2026-03-06 - Issue #171 Onboarding account registration step

## Summary
Added a two-step create-family onboarding flow so account credentials are validated first, then family configuration is completed with inline API feedback.

## Desktop changes
- Added a dedicated onboarding registration validator:
  - `OnboardingRegistrationValidator.ValidateAccountStep(...)`
  - `OnboardingRegistrationValidator.ValidateFamilyStep(...)`
- Updated `CreateFamilyAccountWindow` UI into deterministic steps:
  - Step 1: guardian first/last name, email, password, confirm password
  - Step 2: family name
  - Added `Back`, `Next`, and `Create Account` flow controls
  - Added step description text and inline validation surface
- Updated `CreateFamilyAccountWindow` behavior:
  - blocked step advance when account validation fails
  - blocked final submit until family step is active and valid
  - preserved backend handoff via existing `families/onboard` onboarding path
  - surfaced backend errors inline on failed create

## Tests
- Added `OnboardingRegistrationValidatorTests`:
  - invalid email rejection
  - password mismatch rejection
  - valid account input acceptance
  - missing family name rejection

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test DragonEnvelopes.sln`
