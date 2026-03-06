# Issue #181 - Onboarding persistence/resume reliability and coverage

## Date
- 2026-03-06

## Summary
- Added onboarding auth/isolation integration coverage:
  - user A cannot read family B onboarding profile (`GET /families/{familyId}/onboarding`).
- Added desktop onboarding smoke/reliability coverage:
  - restart-resume test confirms recreated wizard resumes from next incomplete step using persisted onboarding profile state.
  - full happy-path smoke test drives wizard from profile setup through review and verifies dashboard launch event.
  - existing guard tests now cover provider-step failure boundaries (Plaid, Stripe, Cards, Automation) for deterministic progression.

## Validation
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -v minimal` (pass)
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal` (pass)

## Notes
- Resume behavior remains driven by authoritative onboarding profile milestone state and deterministic next-incomplete-step selection.
