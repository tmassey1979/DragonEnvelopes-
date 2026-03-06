# Changelog - 2026-03-05 - Issue #118

## Summary
Implemented provider-agnostic family invite persistence and lifecycle APIs with token-hash storage and status transitions.

## Changes
- Added domain invite model:
  - `FamilyInvite`, `FamilyInviteStatus`
  - lifecycle methods: `Accept`, `Cancel`, `Expire`
- Added contracts:
  - `CreateFamilyInviteRequest`
  - `AcceptFamilyInviteRequest`
  - `FamilyInviteResponse`
  - `CreateFamilyInviteResponse`
- Added application layer:
  - `IFamilyInviteService`, `FamilyInviteService`
  - `IFamilyInviteRepository`
  - DTOs: `FamilyInviteDetails`, `CreateFamilyInviteResult`
- Added infrastructure:
  - `FamilyInviteRepository`
  - EF configuration `FamilyInviteConfiguration`
  - DbSet registration in `DragonEnvelopesDbContext`
  - migration `AddFamilyInvites`
- Added API endpoints:
  - `POST /api/v1/families/{familyId}/invites`
  - `GET /api/v1/families/{familyId}/invites`
  - `POST /api/v1/families/{familyId}/invites/{inviteId}/cancel`
  - `POST /api/v1/families/invites/accept` (anonymous)
- Added validation:
  - create invite request validation (email/role/expiry bounds)
  - accept invite token validation
- Added integration tests:
  - create/list/cancel invite flow
  - cross-family create forbidden behavior
  - anonymous token acceptance

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`
- Result: build succeeded; integration tests passed (12/12).
