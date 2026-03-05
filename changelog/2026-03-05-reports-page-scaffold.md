# 2026-03-05 - Reports Page Scaffold and Endpoint Bindings

## Summary

Implemented a dedicated Reports page view model with filter-aware endpoint bindings and fallback empty-state behavior when reporting data is unavailable.

## Completed Story

- #89 Implement Reports page scaffold with reporting endpoint bindings

## Key Changes

- Added reports data service abstraction and result model:
  - `IReportsDataService`
  - `ReportsDataService`
  - `ReportSummaryData`
  - `client/DragonEnvelopes.Desktop/Services/*.cs`
- Added `ReportsViewModel`:
  - async load/apply filter commands
  - filter fields (`SelectedMonth`, `IncludeArchived`)
  - state fields (`IsLoading`, `HasError`, `ErrorMessage`, `IsEmpty`)
  - summary tile binding collection
  - `client/DragonEnvelopes.Desktop/ViewModels/ReportsViewModel.cs`
- Integrated reports route into navigation:
  - `/reports` now maps to `ReportsViewModel` via reports data service
  - `client/DragonEnvelopes.Desktop/Navigation/RouteRegistry.cs`
- Added reports UI template:
  - filter controls + apply command
  - report summary card with loading/error/empty support
  - bound reusable metric tiles for report output
  - `client/DragonEnvelopes.Desktop/Resources/Templates/ShellTemplates.xaml`

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
