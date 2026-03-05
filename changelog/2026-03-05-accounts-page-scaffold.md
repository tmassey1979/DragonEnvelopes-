# 2026-03-05 - Accounts Page Scaffold and API Bindings

## Summary

Added a dedicated Accounts page view model and data service with API-backed account loading, plus loading/error/empty state presentation and placeholder action hooks.

## Completed Story

- #88 Implement Accounts page scaffold and API bindings

## Key Changes

- Added accounts data service abstraction:
  - `IAccountsDataService`
  - `AccountsDataService` (calls desktop API client and maps contract responses)
  - `client/DragonEnvelopes.Desktop/Services/*.cs`
- Added accounts page view models:
  - `AccountsViewModel` with async load command and state handling
  - `AccountListItemViewModel`
  - `client/DragonEnvelopes.Desktop/ViewModels/*.cs`
- Integrated accounts page into route registry:
  - Accounts route now maps to `AccountsViewModel`
  - Reuses centralized authenticated API client pipeline
  - `client/DragonEnvelopes.Desktop/Navigation/RouteRegistry.cs`
  - `client/DragonEnvelopes.Desktop/ViewModels/MainWindowViewModel.cs`
- Added accounts-specific UI template:
  - Account list rendering
  - loading/error/empty state support
  - placeholder command for future account actions
  - `client/DragonEnvelopes.Desktop/Resources/Templates/ShellTemplates.xaml`

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
