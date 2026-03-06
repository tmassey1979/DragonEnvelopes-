# Changelog - 2026-03-05 - Issue #119

## Summary
Added desktop family invite management UI integrated with backend invite APIs, including dev-friendly token visibility.

## Changes
- Extended family members data service to support invite APIs:
  - list invites
  - create invite
  - cancel invite
- Added desktop invite models:
  - `FamilyInviteItemViewModel`
  - `CreateFamilyInviteResultData`
- Extended `FamilyMembersViewModel`:
  - invite draft fields (email/role/expiry hours)
  - invite commands (create/cancel)
  - invite collection + selection
  - invite status message and last created token display
- Updated family template UI:
  - new invite section under Family Access card
  - invite creation controls
  - invite list grid with status and expiry
  - dev token display text for local onboarding/testing

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj -nologo -p:OutputPath=bin/TempVerify/`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`
- Result: desktop build succeeded (0 warnings/errors); integration tests passed (12/12).
