# 2026-03-05 - Dark Theme Dictionaries and Base Styles

## Summary

Introduced merged resource dictionaries for dark palette, typography, and controls, then applied them globally across the desktop shell and content templates.

## Completed Story

- #55 Create dark theme resource dictionaries and base typography styles

## Key Changes

- Added maintainable merged dictionaries:
  - `client/DragonEnvelopes.Desktop/Resources/Themes/Colors.Dark.xaml`
  - `client/DragonEnvelopes.Desktop/Resources/Themes/Typography.xaml`
  - `client/DragonEnvelopes.Desktop/Resources/Themes/Controls.xaml`
  - `client/DragonEnvelopes.Desktop/Resources/Templates/ShellTemplates.xaml`
- Updated app resource bootstrap:
  - `App.xaml` now merges theme/template dictionaries globally
  - `client/DragonEnvelopes.Desktop/App.xaml`
- Added global style foundations:
  - semantic dark brushes
  - base/heading/body/caption text styles
  - base button, sidebar nav button, and text input styles
- Applied theme/styles through shell UI:
  - `MainWindow.xaml` updated to consume global resources/styles and remove inline styling duplication

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
