# Issue #229: Family Invite Resend (API + Desktop)

## Summary
- Added resend support for pending family invites across monolith API, Family API, and desktop family management UI.
- Added request contract and validation for resend expiration input.
- Added test coverage for API auth boundaries, Family API smoke flow, and desktop resend command behavior.

## Delivered
- Contract:
  - `ResendFamilyInviteRequest` under `DragonEnvelopes.Contracts.Families`.
- Domain/Application:
  - Added `FamilyInvite.Resend(...)`.
  - Added `IFamilyInviteService.ResendAsync(...)` and implementation in `FamilyInviteService`.
- API endpoints:
  - `POST /api/v1/families/{familyId}/invites/{inviteId}/resend` in monolith API.
  - `POST /api/v1/families/{familyId}/invites/{inviteId}/resend` in Family API.
- Validation:
  - Added `ResendFamilyInviteRequestValidator` in both API projects (1-720 hours).
- Desktop:
  - Added resend operation in `IFamilyMembersDataService`/`FamilyMembersDataService`.
  - Added `ResendInviteCommand` in `FamilyMembersViewModel`.
  - Added `Resend Selected` action in family members invite UI.
  - Updated fake data service for resend behavior in tests.

## Verification
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -v minimal --filter "FullyQualifiedName~UserA_Can_Resend_Pending_Invite|FullyQualifiedName~UserA_Cannot_Resend_Invite_For_FamilyB"`
- `dotnet test tests/DragonEnvelopes.Family.Api.IntegrationTests/DragonEnvelopes.Family.Api.IntegrationTests.csproj -v minimal --filter "FullyQualifiedName~Authenticated_User_Can_Create_And_Resend_Invite_For_Own_Family"`
