# Issue #185 - Invite Onboarding Registration from Invite Token

## Delivered
- Added invite registration contracts:
  - `RegisterFamilyInviteAccountRequest`
  - `RegisterFamilyInviteAccountResponse`
- Added API validation for invite registration payload:
  - token, names, email, password constraints.
- Added anonymous API endpoint:
  - `POST /api/v1/families/invites/register`
  - provisions Keycloak user, redeems invite to family member, assigns mapped realm role.
  - includes best-effort compensation to delete newly-created Keycloak users when redeem fails.
- Added desktop service support for invite registration flow:
  - `IFamilyAccountService.RegisterFromInviteAsync(...)`
  - request model `RegisterFamilyInviteAccountRequestData`
- Added desktop invite registration UX:
  - `Create From Invite` action from login control.
  - dedicated `CreateInviteAccountWindow` for token + account details.
  - automatic sign-in after successful invite registration and family-context refresh.
- Added invite registration validation helper:
  - `OnboardingRegistrationValidator.ValidateInviteRegistrationStep(...)`.
- Added integration coverage for invite registration scenarios:
  - happy path creates account and family member
  - duplicate Keycloak email rejection
  - cancelled invite rejection
  - expired invite rejection
- Hardened integration host determinism:
  - replaced real `IKeycloakProvisioningService` with in-memory `TestKeycloakProvisioningService` in `TestApiFactory`.

## Validation
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --filter "Invite_Registration" -v minimal`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -v minimal`
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass

## Notes
- Invite registration failure-path responses align with domain validation handling and return `422 UnprocessableEntity`.
