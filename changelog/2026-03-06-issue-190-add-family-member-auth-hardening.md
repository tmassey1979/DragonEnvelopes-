# Issue #190 - Add Family Member Auth Hardening

## Delivered
- Secured `POST /api/v1/families/{familyId}/members` by replacing anonymous access with parent authorization.
- Added an explicit family-access guard on the add-member endpoint to prevent cross-family member creation attempts.
- Added integration tests that validate:
  - unauthorized requests return `401`
  - parent users can add members to their own family
  - users are forbidden (`403`) from adding members to another family
- Removed provider-specific query usage in family repository existence checks to keep integration tests provider compatible:
  - replaced `EF.Functions.ILike` checks with normalized case-insensitive equality comparisons.

## Validation
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --filter "Unauthorized_Request_Cannot_Add_Family_Member|UserA_Can_Add_Family_Member_To_Own_Family|UserA_Cannot_Add_Family_Member_To_FamilyB" -v minimal`
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
