# Issue #207 - Family API Add-Member Auth Hardening

## Delivered
- Hardened `POST /families/{familyId:guid}/members` in `DragonEnvelopes.Family.Api`:
  - Added caller family-scope guard using `EndpointAccessGuards.UserHasFamilyAccessAsync`.
  - Changed endpoint authorization from anonymous access to `ApiAuthorizationPolicies.Parent`.
- Preserved response payload and successful behavior for authorized callers.

## Validation
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
