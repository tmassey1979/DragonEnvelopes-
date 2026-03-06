# 2026-03-06 - Role-Aware Route Gating and Parent-Only UX Guard

## Summary
Implemented role-aware navigation gating in desktop UI using roles from `auth/me`, including parent-only route access control.

## Changes
- Added optional `RequiredRole` metadata to route definitions.
- Marked Family Members route as parent-only (`RequiredRole = Parent`).
- Extended navigation item view model with `IsEnabled` and required-role awareness.
- Updated main shell logic to:
  - parse user roles from `auth/me`
  - expose role summary in top bar
  - apply route enable/disable state based on role
  - auto-navigate back to dashboard if currently selected route becomes unauthorized
- Updated sidebar item control/template to respect disabled navigation state.

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj -p:OutputPath=bin/TempVerify/` (pass)

## Notes
- This provides clear UX guardrails before backend authorization denies requests.
- Backend policies remain source-of-truth for security.
