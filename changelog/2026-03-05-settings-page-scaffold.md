# 2026-03-05 - Settings Page Scaffold with Session/Profile Controls

## Summary

Implemented a dedicated settings page scaffold including session controls, profile placeholders, and runtime metadata display.

## Completed Story

- #90 Implement Settings page scaffold with session/profile controls

## Key Changes

- Added `SettingsViewModel`:
  - session status load and refresh command
  - sign-out command
  - profile placeholder messaging
  - app version/environment metadata fields
  - `client/DragonEnvelopes.Desktop/ViewModels/SettingsViewModel.cs`
- Updated route registry wiring:
  - `/settings` now maps to `SettingsViewModel`
  - route registry now receives shared auth service dependency
  - `client/DragonEnvelopes.Desktop/Navigation/RouteRegistry.cs`
  - `client/DragonEnvelopes.Desktop/ViewModels/MainWindowViewModel.cs`
- Added settings-specific template sections:
  - session controls card
  - profile placeholders card
  - metadata card
  - `client/DragonEnvelopes.Desktop/Resources/Templates/ShellTemplates.xaml`

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
