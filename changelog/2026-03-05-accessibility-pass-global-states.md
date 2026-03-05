# 2026-03-05 - Accessibility Pass and Global State UX Improvements

## Summary
Completed a desktop accessibility and UX-state pass focused on keyboard access, focus visibility, readable control sizing, and consistent loading/empty state announcements.

## Changes
- Updated shared control theme styles for accessibility across the app:
  - Added keyboard focus visual style.
  - Added focused border states for Button/TextBox/PasswordBox/ComboBox.
  - Added minimum control heights for touch/readability.
  - Added themed styles for ComboBox, CheckBox, and DataGrid (including selected row/header states).
- Improved global loading/empty state accessibility in shared controls:
  - `CardControl` loading and empty text now uses polite live announcements.
  - `MetricTileControl` loading and empty text now uses polite live announcements.
- Improved login/create-family forms accessibility:
  - Added label-target pairings and mnemonic access keys.
  - Added explicit automation names to input/actions.
  - Added default/cancel button semantics for keyboard-first flows.
  - Added polite live status text for validation/status messages.
  - Dialog windows now avoid extra taskbar clutter (`ShowInTaskbar=false`).
- Added accessibility naming for key top-bar controls in main window.

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj` (pass)

## Notes
- These changes are shared-style driven, so existing and future pages inherit the accessibility improvements by default.
