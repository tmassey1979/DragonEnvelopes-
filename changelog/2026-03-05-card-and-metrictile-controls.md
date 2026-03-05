# 2026-03-05 - Reusable Card and MetricTile Controls

## Summary

Added reusable WPF `CardControl` and `MetricTileControl` components with dependency properties and built-in loading/empty states, then integrated them into shell templates.

## Completed Story

- #56 Build reusable Card and MetricTile controls

## Key Changes

- Added reusable controls:
  - `client/DragonEnvelopes.Desktop/Controls/CardControl.xaml`
  - `client/DragonEnvelopes.Desktop/Controls/CardControl.xaml.cs`
  - `client/DragonEnvelopes.Desktop/Controls/MetricTileControl.xaml`
  - `client/DragonEnvelopes.Desktop/Controls/MetricTileControl.xaml.cs`
- Added metric view models and trend model:
  - `MetricTileViewModel`
  - `MetricTrendDirection`
  - `client/DragonEnvelopes.Desktop/ViewModels/MetricTileViewModel.cs`
  - `client/DragonEnvelopes.Desktop/ViewModels/MetricTrendDirection.cs`
- Extended shell content model for reusable states:
  - Added `Metrics`, `IsLoading`, and `IsEmpty` to `ShellContentViewModel`
  - `client/DragonEnvelopes.Desktop/ViewModels/ShellContentViewModel.cs`
- Integrated controls into template composition:
  - Updated `ShellTemplates.xaml` to render metric tile collection + main card
  - `client/DragonEnvelopes.Desktop/Resources/Templates/ShellTemplates.xaml`
- Populated route registry with metric and state data:
  - Dashboard shows populated metrics
  - Other routes demonstrate loading/empty visuals
  - `client/DragonEnvelopes.Desktop/Navigation/RouteRegistry.cs`

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
