# Issue #239: Settings Auth Session Diagnostics and Recovery

## Summary
- Expanded Settings session controls with auth diagnostics and guided recovery actions.
- Added session detail visibility (user, expiry, refresh token availability) without exposing sensitive token values.
- Added diagnostic event timeline and command-state test coverage.

## Delivered
- Desktop view models:
  - Added `AuthDiagnosticEventItemViewModel`.
  - Extended `SettingsViewModel` with:
    - `SessionUser`
    - `SessionExpiresAtUtcDisplay`
    - `SessionRefreshTokenStatus`
    - `AuthRecoveryMessage`
    - `AuthDiagnostics`
  - Added commands:
    - `RefreshSessionNowCommand`
    - `ClearSessionCommand`
    - `ReauthenticateGuidanceCommand`
- Desktop UI:
  - Updated Session Controls card with detailed diagnostics fields.
  - Added recovery action buttons and auth diagnostic timeline grid.
- Tests:
  - Added settings tests for refresh-session, clear-session, and re-auth guidance command behavior.

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
- `dotnet build DragonEnvelopes.sln -v minimal`
