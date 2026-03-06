# Changelog - 2026-03-05 - Issue #114

## Summary
Added backend runtime health/version endpoints and surfaced backend runtime status in desktop Settings.

## Changes
- Added contracts:
  - `src/DragonEnvelopes.Contracts/Runtime/ApiHealthResponse.cs`
  - `src/DragonEnvelopes.Contracts/Runtime/ApiVersionResponse.cs`
- Added API endpoints in `src/DragonEnvelopes.Api/Program.cs`:
  - `GET /api/v1/system/health` (anonymous)
  - `GET /api/v1/system/version` (anonymous)
- Added desktop system status service:
  - `ISystemStatusDataService`
  - `SystemStatusDataService`
  - `SystemRuntimeStatusData`
- Updated desktop settings flow:
  - `SettingsViewModel` now loads session + backend status together
  - Added backend health/version/environment/last-check properties
  - Refresh action updated to refresh full status set
- Updated UI template:
  - Settings metadata card now shows backend health/version/environment/check timestamp/status message
- Added integration tests:
  - anonymous health endpoint availability
  - anonymous version endpoint availability + payload shape checks

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj -nologo`
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj -nologo -p:OutputPath=bin/TempVerify/`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`
- Result: all passed; integration tests passed 6/6.
