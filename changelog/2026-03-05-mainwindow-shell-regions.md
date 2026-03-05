# 2026-03-05 - Desktop MainWindow Shell Regions

## Summary

Built the WPF shell foundation with persistent sidebar/topbar regions and a bindable content host that swaps page content via `CurrentContent` binding.

## Completed Story

- #53 Build MainWindow shell regions (sidebar, topbar, content host)

## Key Changes

- Added MVVM toolkit dependency for desktop:
  - `client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj`
- Introduced shell view models:
  - `MainWindowViewModel` with nav items, selected state, and `CurrentContent`
  - `NavigationItemViewModel`
  - `ShellContentViewModel`
  - `client/DragonEnvelopes.Desktop/ViewModels/*.cs`
- Rebuilt `MainWindow.xaml` as a three-region shell:
  - Persistent sidebar navigation
  - Persistent topbar section header
  - Main content `ContentControl` bound to `CurrentContent`
  - `client/DragonEnvelopes.Desktop/MainWindow.xaml`
- Added application resources and data templates:
  - Shell color resources
  - `DataTemplate` for `ShellContentViewModel` rendering in content host
  - `client/DragonEnvelopes.Desktop/App.xaml`
- Removed template-generated unused code-behind imports:
  - `client/DragonEnvelopes.Desktop/MainWindow.xaml.cs`
  - `client/DragonEnvelopes.Desktop/App.xaml.cs`

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
