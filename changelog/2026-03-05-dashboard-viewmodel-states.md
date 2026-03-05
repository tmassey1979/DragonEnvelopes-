# 2026-03-05 - Dashboard ViewModel with KPI Bindings and States

## Summary

Added a dedicated `DashboardViewModel` with async load behavior, KPI card bindings, recent transaction bindings, and loading/error/empty state handling.

## Completed Story

- #61 Implement Dashboard view model with KPI card bindings and states

## Key Changes

- Added `DashboardViewModel`:
  - Async `LoadCommand` with cancellation token support
  - KPI card collection (`KpiCards`)
  - Recent transaction collection (`RecentTransactions`)
  - State fields: `IsLoading`, `HasError`, `ErrorMessage`, `IsEmpty`
  - `client/DragonEnvelopes.Desktop/ViewModels/DashboardViewModel.cs`
- Added dedicated dashboard data template:
  - Dashboard header + reload action
  - KPI card section bound to reusable `MetricTileControl`
  - Recent transaction section bound to reusable `TransactionRowControl`
  - Loading/error/empty state visuals through `CardControl`
  - `client/DragonEnvelopes.Desktop/Resources/Templates/ShellTemplates.xaml`
- Updated route/navigation model to allow heterogeneous content view models:
  - Route content type now supports `object` (not only shell placeholder model)
  - Dashboard route now maps to `DashboardViewModel`
  - `RouteDefinition`, `INavigationService`, `NavigationService`, `NavigationItemViewModel`, `MainWindowViewModel`, `RouteRegistry`
  - `client/DragonEnvelopes.Desktop/Navigation/*.cs`
  - `client/DragonEnvelopes.Desktop/ViewModels/*.cs`

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
