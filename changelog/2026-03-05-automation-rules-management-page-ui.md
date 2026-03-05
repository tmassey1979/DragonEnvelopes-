# 2026-03-05 - Automation Rules Management Page UI

## Summary
Implemented a full desktop Automation Rules page with API-backed CRUD and enable/disable/delete actions.

## Changes
- Replaced automation placeholder route with `AutomationRulesViewModel`.
- Added automation rules data layer:
  - `IAutomationRulesDataService`
  - `AutomationRulesDataService`
- Wired API workflows for:
  - list rules with filters
  - create rule
  - update rule
  - enable/disable rule
  - delete rule
- Added automation rule list/editor view models.
- Added dedicated XAML template with:
  - filter bar (type + enabled)
  - rule grid (priority order)
  - editor for name/type/priority/enabled and JSON condition/action payloads
  - New/Save/Enable-Disable/Delete actions
- Added client-side editor validation for required fields, priority floor, and JSON object checks.

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj` (pass)

## Notes
- Rule list ordering follows API deterministic precedence: `Priority ASC` then creation order.
- JSON payloads are validated client-side before submit to reduce avoidable API validation errors.
