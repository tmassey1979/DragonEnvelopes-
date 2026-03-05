# 2026-03-05 - SidebarItem and EnvelopeTile Reusable Controls

## Summary

Implemented reusable sidebar and envelope tile controls with hover/focus/selected states and integrated them into shared shell templates.

## Completed Story

- #57 Build reusable SidebarItem and EnvelopeTile controls

## Key Changes

- Added `SidebarItemControl`:
  - Icon/label/selected state via dependency properties
  - Command + command parameter support
  - Keyboard focus and selected-state visual behavior
  - `client/DragonEnvelopes.Desktop/Controls/SidebarItemControl.xaml`
  - `client/DragonEnvelopes.Desktop/Controls/SidebarItemControl.xaml.cs`
- Added `EnvelopeTileControl`:
  - Name, budget, balance, selected-state dependency properties
  - Hover/focus/selected visuals for reusable envelope cards
  - `client/DragonEnvelopes.Desktop/Controls/EnvelopeTileControl.xaml`
  - `client/DragonEnvelopes.Desktop/Controls/EnvelopeTileControl.xaml.cs`
- Added envelope tile view model:
  - `client/DragonEnvelopes.Desktop/ViewModels/EnvelopeTileViewModel.cs`
- Updated shell model and route registry:
  - `ShellContentViewModel` now exposes envelope tile collections
  - `RouteRegistry` provides sample envelope tiles for dashboard/envelopes
  - `client/DragonEnvelopes.Desktop/ViewModels/ShellContentViewModel.cs`
  - `client/DragonEnvelopes.Desktop/Navigation/RouteRegistry.cs`
- Integrated controls into UI composition:
  - Sidebar now uses `SidebarItemControl` in `MainWindow`
  - Card content template renders `EnvelopeTileControl` items
  - `client/DragonEnvelopes.Desktop/MainWindow.xaml`
  - `client/DragonEnvelopes.Desktop/Resources/Templates/ShellTemplates.xaml`

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
