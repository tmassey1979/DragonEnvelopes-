# 2026-03-06 - Issue #166 Onboarding reconcile operation

## Summary
Added a deterministic onboarding reconcile operation that recalculates milestone progress from real family data and surfaces it in both API and desktop onboarding UI.

## Backend/API changes
- Added onboarding milestone signal DTO:
  - `OnboardingMilestoneSignalsDetails`
- Extended onboarding profile repository contract and implementation with:
  - `GetMilestoneSignalsAsync(familyId)`
- Added onboarding service operation:
  - `ReconcileAsync(familyId)`
- Added API route:
  - `POST /api/v1/families/{familyId}/onboarding/reconcile`
- Added same reconcile route in `DragonEnvelopes.Family.Api` for service parity.
- Reconcile rules implemented:
  - members >= 2
  - account exists
  - envelope exists
  - budget exists
  - plaid link exists
  - stripe financial account exists
  - card exists
  - automation rule exists

## Desktop changes
- Added onboarding data service operation:
  - `ReconcileProfileAsync()`
- Added onboarding wizard command:
  - `ReconcileProgressCommand`
- Added onboarding UI `Reconcile` action button.

## Tests
- Added API integration tests:
  - own-family reconcile success with explicit seeded data
  - cross-family reconcile forbidden
- Extended desktop onboarding tests:
  - reconcile command updates status and state
- Updated onboarding step progression assertion to match deterministic next-step behavior after profile refresh.

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj`
