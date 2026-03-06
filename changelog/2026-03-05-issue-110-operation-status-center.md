# Changelog - 2026-03-05 - Issue #110

## Summary
Implemented a global desktop shell operation status center for async actions and hardened auth token refresh failure handling.

## Changes
- Added `OperationStatusCenter` service with:
  - active operation tracking (`BeginOperation` / scope disposal)
  - toast pipeline (`Info`, `Success`, `Error`)
  - automatic transient toast cleanup and max history cap
- Added `OperationToastItemViewModel` and toast severity model.
- Extended `MainWindowViewModel` to:
  - expose `OperationStatusCenter`
  - report sign-in/sign-out/ping/family-context operations and outcomes
  - observe current route content for `IsLoading` + `HasError`/`ErrorMessage`
  - publish global error toasts and operation activity from screen viewmodels
- Updated `MainWindow.xaml` with a shell-level status panel:
  - current operation summary
  - recent toast list
  - clear transient notifications action
- Hardened `DesktopAuthService.EnsureSessionAsync` refresh flow with guarded refresh call + fallback expiry seconds.

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj -nologo -p:OutputPath=bin/TempVerify/`
- Result: build succeeded (0 errors, 0 warnings).
