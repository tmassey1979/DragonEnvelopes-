# Issue #174 - Onboarding budget preferences step

## Date
- 2026-03-06

## Summary
- Added family budget preferences persistence across domain, application, infrastructure, API, and desktop client layers.
- Added budget-preferences onboarding UI controls for pay frequency, budgeting style, optional household monthly income, and save action.
- Added budget-preference summary rendering in the onboarding wizard budget step.
- Added API auth/isolation integration coverage for budget-preferences endpoints.
- Added desktop view model test coverage for budget-preferences save + milestone completion flow.
- Added EF Core migration `AddFamilyBudgetPreferences` for `families` table columns:
  - `PayFrequency`
  - `BudgetingStyle`
  - `HouseholdMonthlyIncome`

## Validation
- `dotnet build DragonEnvelopes.sln -v minimal` (pass)
- `dotnet test DragonEnvelopes.sln -v minimal` (pass)

## Notes
- Onboarding reconcile milestone logic now considers saved budget preferences as satisfying budget setup readiness.
