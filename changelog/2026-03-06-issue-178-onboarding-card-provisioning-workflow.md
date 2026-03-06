# Issue #178 - Onboarding card provisioning workflow

## Date
- 2026-03-06

## Summary
- Added onboarding Cards step UI and behavior for:
  - virtual card issuance
  - physical card issuance with shipping capture
  - issued-card list and selection per provisioned envelope
  - physical issuance refresh/status display
  - post-issue card control defaults save (daily limit, allowed categories, allowed merchants)
- Added envelope selection for card provisioning based on Stripe-provisioned envelopes.
- Added validation for physical shipping fields before physical card API submit.
- Added completion gating so card step requires at least one issued card.
- Added desktop unit test coverage for card-step completion guard.

## Validation
- `dotnet build DragonEnvelopes.sln -v minimal` (pass)
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal` (pass)

## Notes
- Card workflows reuse existing financial integration API services and keep provider identifiers masked in UI displays.
