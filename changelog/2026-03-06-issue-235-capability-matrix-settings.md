# Issue #235: Desktop Capability Matrix in Settings

## Summary
- Added a capability matrix panel in Settings to show backend-feature coverage in the desktop app.
- Added a local capability manifest model in `SettingsViewModel` with status summaries.
- Added desktop tests to validate matrix loading and domain coverage.

## Delivered
- Desktop view model:
  - Added `CapabilityMatrixItemViewModel`.
  - Extended `SettingsViewModel` with:
    - `CapabilityMatrix`
    - `CapabilitySummary`
    - local manifest builder covering Family/Ledger/Financial/System capabilities.
- Desktop UI:
  - Added capability matrix section to Settings metadata card.
  - Added DataGrid columns for domain, capability, status, and notes.
- Tests:
  - Expanded `SettingsViewModelTests` to assert matrix population and expected domain coverage.

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
- `dotnet build DragonEnvelopes.sln -v minimal`
