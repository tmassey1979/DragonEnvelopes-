# Issue #180 - Onboarding finish summary and dashboard handoff

## Date
- 2026-03-06

## Summary
- Added onboarding Review step UI with:
  - completed capability summary
  - incomplete optional capability list
  - completion timestamp display
  - Launch Dashboard action
- Added review summary state generation from onboarding profile/milestones.
- Added dashboard handoff event from onboarding wizard and wired MainWindow navigation to route to `/dashboard` when triggered.
- Added desktop test coverage for launch-dashboard event behavior.

## Validation
- `dotnet build DragonEnvelopes.sln -v minimal` (pass)
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal` (pass)

## Notes
- Completion timestamp is sourced from onboarding profile `CompletedAtUtc` and displayed in the review panel.
- Completion still requires explicit user interactions through milestone actions; no background auto-complete workflow was introduced.
