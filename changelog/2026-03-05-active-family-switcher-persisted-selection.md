# 2026-03-05 - Active Family Switcher with Persisted Selection

## Summary
Implemented desktop active-family switching for multi-family users, including persisted selection and family-bound page refresh behavior.

## Changes
- Added top-bar family picker to main shell.
- Added `FamilyOptionViewModel` for family selector items.
- Added persisted family selection store:
  - `IFamilySelectionStore`
  - `ProtectedFamilySelectionStore`
- Updated `MainWindowViewModel` to:
  - Load family IDs from `auth/me`
  - Resolve family display names via `GET /families/{id}`
  - Restore persisted family selection when valid
  - Save selected family on change
  - Refresh family-bound viewmodels (accounts/envelopes/transactions/budgets/automation) when family changes
  - Clear family selection on sign-out/no-family state

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj -p:OutputPath=bin/TempVerify/` (pass)

## Notes
- Alternate output path used for validation because normal debug output can be locked when app is running.
