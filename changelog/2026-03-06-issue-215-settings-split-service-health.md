# Issue #215 - Desktop Settings: show split-service health and remove placeholder copy

## Summary
Updated desktop Settings so operators can verify split-backend health (API, Family API, Ledger API) directly in the runtime metadata panel, and removed stale placeholder wording.

## Delivered
- Added split-service health fields to runtime status model:
  - `client/DragonEnvelopes.Desktop/Services/SystemRuntimeStatusData.cs`
- Extended desktop runtime status probing:
  - `client/DragonEnvelopes.Desktop/Services/SystemStatusDataService.cs`
  - probes `DRAGONENVELOPES_FAMILY_API_HEALTH_URL` (default `http://localhost:18089/health/ready`)
  - probes `DRAGONENVELOPES_LEDGER_API_HEALTH_URL` (default `http://localhost:18090/health/ready`)
  - probe failures are handled non-fatally and surfaced as status messages
- Updated settings view model bindings:
  - `client/DragonEnvelopes.Desktop/ViewModels/SettingsViewModel.cs`
- Updated Settings UI template:
  - `client/DragonEnvelopes.Desktop/Resources/Templates/ShellTemplates.xaml`
  - removed placeholder copy and added Family/Ledger health + status lines
- Updated docs for desktop env-var overrides:
  - `README.md`
- Updated desktop tests:
  - `tests/DragonEnvelopes.Desktop.Tests/SettingsViewModelTests.cs`

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal`
- `dotnet build DragonEnvelopes.sln -v minimal`

All validation commands completed successfully.
