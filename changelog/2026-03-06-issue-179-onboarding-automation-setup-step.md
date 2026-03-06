# Issue #179 - Onboarding automation setup step

## Date
- 2026-03-06

## Summary
- Added onboarding Automation step UI with starter templates for:
  - categorization rules
  - allocation rule requiring selected envelope
- Added template selection and creation flow to persist rules through existing automation APIs.
- Added in-step automation rule list with concise row summary (name/type/priority/enabled/updated).
- Added enable/disable toggle for selected onboarding automation rule.
- Added allocation-envelope selection options derived from provisioned envelopes.
- Added completion guard so automation step requires at least one created rule.
- Added desktop test coverage for automation-step completion guard.

## Validation
- `dotnet build DragonEnvelopes.sln -v minimal` (pass)
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal` (pass)

## Notes
- Rule creation skips duplicate template names already present in family automation rules.
