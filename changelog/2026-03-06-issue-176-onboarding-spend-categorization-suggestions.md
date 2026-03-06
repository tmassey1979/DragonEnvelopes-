# Issue #176 - Onboarding spend categorization review and envelope suggestions

## Date
- 2026-03-06

## Summary
- Added onboarding spend review tooling inside the Envelopes step:
  - load recent category spend distribution from reports data
  - generate deterministic envelope suggestions from category totals
  - edit suggested envelope names and monthly budgets
  - remove individual suggestions
  - merge duplicate-name suggestions
  - apply approved suggestions by creating envelopes via API
- Added supportability decision trail in the wizard (`SpendSuggestionDecisionEvents`) for generation/remove/merge/apply actions.
- Extended onboarding route wiring to provide reports/envelopes services to the wizard.

## Validation
- `dotnet build DragonEnvelopes.sln -v minimal` (pass)
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal` (pass)

## Notes
- Suggestion generation uses a deterministic heuristic (recent category totals, sorted descending) to keep behavior consistent in v1.
