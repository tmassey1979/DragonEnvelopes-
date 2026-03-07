# Issue 244 - Provider Timeline UX Take and Clear Filters

## Summary
Improved desktop provider timeline filtering UX by adding explicit event-count control and one-click filter reset.

## Delivered
- Updated provider timeline controls in desktop UI:
  - Added `Take` input field (requested timeline event count).
  - Added `Clear Filters` action that resets source/status/take and reloads timeline.
- Updated `FinancialIntegrationsViewModel`:
  - Added `ProviderTimelineTake` state (default `25`).
  - Added `ClearProviderTimelineFiltersCommand`.
  - Added bounded take validation (`1..100`) with user-facing validation messages.
  - Updated timeline loading to pass explicit `take` along with source/status filters.
- Updated desktop test fakes and smoke tests:
  - Fake financial integration data service now tracks last requested timeline `take`.
  - Extended filter test to assert explicit `take`.
  - Added clear-filters reset flow test.
  - Added invalid-take validation test.
- Updated `Codex/TaskLifecycleChecklist.md` active task log for `#244`.

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --configuration Release --no-restore`
  - Passed: 79, Failed: 0
