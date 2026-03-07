# Issue #183 - Family Invite Redemption and Membership Linking

## Delivered
- Added authenticated invite redemption flow that links invited users to family membership.
- Added new contracts:
  - `RedeemFamilyInviteRequest`
  - `RedeemFamilyInviteResponse`
- Added new application DTO:
  - `FamilyInviteRedemptionDetails`
- Extended invite service:
  - `IFamilyInviteService.RedeemAsync(...)`
  - duplicate/idempotent handling for existing member by Keycloak user id
  - email validation against invite email
  - role-to-member mapping on member creation
- Added API endpoint:
  - `POST /api/v1/families/invites/redeem` (authenticated)
- Desktop login flow updates:
  - Added "Redeem Invite" action in login control.
  - Added `RedeemFamilyInviteWindow` for token entry.
  - Added invite redemption call via `IFamilyAccountService`.
  - Added post-redemption family context refresh in `MainWindowViewModel`.
- Added integration tests for:
  - authentication requirement
  - successful redemption/member creation
  - idempotent repeat redemption behavior

## Validation
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass

## Notes
- Existing anonymous `/families/invites/accept` endpoint remains intact for backward compatibility.
- New redemption route is intended for authenticated onboarding/sign-in flows.
