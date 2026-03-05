# 2026-03-05 - Envelopes UI (List + Detail Create/Edit/Archive) (#62)

## Summary
- Implemented a dedicated WPF Envelopes workspace (replacing placeholder content) with:
  - envelope list
  - detail editor
  - create new envelope flow
  - edit/save flow
  - archive action
- Added desktop envelopes data service + interface:
  - `IEnvelopesDataService`
  - `EnvelopesDataService`
- Added envelope viewmodels:
  - `EnvelopesViewModel`
  - `EnvelopeListItemViewModel`
- Wired `/envelopes` navigation route to the new Envelopes viewmodel.

## Family Context Wiring
- Added shared current-family context in desktop app:
  - `IFamilyContext`
  - `FamilyContext`
- Updated session/bootstrap flow to resolve family scope from API after sign-in/restore via `/api/v1/auth/me`.
- Updated Accounts service to use required `familyId` query parameter using family context.

## API Adjustment Used by UI
- Extended `GET /api/v1/auth/me` to include `familyIds` for authenticated user membership resolution in desktop client.

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release`
- `docker compose up -d --build api`
- Authenticated smoke verified:
  - `/auth/me` returns `familyIds`
  - envelope create/list/update/archive endpoints function end-to-end
