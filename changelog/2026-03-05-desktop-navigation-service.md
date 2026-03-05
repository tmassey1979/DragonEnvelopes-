# 2026-03-05 - Desktop Navigation Service and Route Registry

## Summary

Added an MVVM navigation abstraction with a route registry and deep-link style keys, then wired MainWindow navigation state to the service.

## Completed Story

- #54 Implement MVVM navigation service and route registry

## Key Changes

- Added navigation contracts and models:
  - `INavigationService`
  - `IRouteRegistry`
  - `RouteDefinition`
  - `client/DragonEnvelopes.Desktop/Navigation/*.cs`
- Added concrete navigation implementations:
  - `NavigationService` for current route/content/title/subtitle state
  - `RouteRegistry` with sidebar route registration using deep-link keys (`/dashboard`, `/envelopes`, etc.)
  - `client/DragonEnvelopes.Desktop/Navigation/NavigationService.cs`
  - `client/DragonEnvelopes.Desktop/Navigation/RouteRegistry.cs`
- Refactored `MainWindowViewModel`:
  - Uses `INavigationService` instead of inline route logic
  - Syncs selected menu state from navigation service
  - Uses route keys for command-based navigation
  - `client/DragonEnvelopes.Desktop/ViewModels/MainWindowViewModel.cs`

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
