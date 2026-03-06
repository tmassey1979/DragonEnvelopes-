# Issue #177 - Onboarding Stripe financial account provisioning

## Date
- 2026-03-06

## Summary
- Added onboarding Stripe provisioning step UI for eligible envelope selection.
- Added per-envelope provisioning workspace with editable display name and row status/detail fields.
- Implemented Stripe provisioning actions:
  - refresh provisioning status
  - provision selected envelopes
  - retry failed rows
- Added partial-failure handling so failed rows do not block successful rows in the same operation.
- Added completion gating so Stripe onboarding step cannot be completed until at least one envelope is provisioned.
- Added desktop test coverage for Stripe-step completion guard.

## Validation
- `dotnet build DragonEnvelopes.sln -v minimal` (pass)
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal` (pass)

## Notes
- Onboarding uses existing financial integration APIs and envelope data services to preserve family isolation and provider abstraction.
