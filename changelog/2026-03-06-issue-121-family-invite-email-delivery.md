# Changelog - 2026-03-06 - Issue #121

## Summary
Added configurable invite email delivery integration with dev fallback logging for family invite creation.

## Changes
- Added application abstraction:
  - `IFamilyInviteSender`
- Updated invite creation flow:
  - `FamilyInviteService` now invokes invite sender after persisting invite
  - failures in outbound delivery do not block invite persistence
- Added infrastructure implementation:
  - `FamilyInviteSender` (SMTP when configured, otherwise structured dev fallback logging)
  - `FamilyInviteEmailOptions` config model
- Updated infrastructure DI:
  - registered `IFamilyInviteSender`
  - added options binding from `FamilyInvites:Email` config section
- Updated API config defaults:
  - added `FamilyInvites:Email` blocks in `appsettings.json` and `appsettings.Development.json`

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`
- Result: build succeeded; integration tests passed (13/13).
