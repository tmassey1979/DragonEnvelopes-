# 2026-03-06 - Issue #165 Onboarding Phase 2 milestone expansion

## Summary
Expanded onboarding milestone tracking from a Phase 1-only profile to a Phase 2 profile that includes member setup, Plaid, Stripe accounts, cards, and automation readiness.

## Backend/API changes
- Expanded onboarding domain model with additional milestone flags:
  - `MembersCompleted`
  - `PlaidCompleted`
  - `StripeAccountsCompleted`
  - `CardsCompleted`
  - `AutomationCompleted`
- Updated onboarding completion logic to require all milestone flags.
- Updated onboarding application DTOs/services and endpoint mappings.
- Updated onboarding update contract payload and response payload to include new fields.
- Added EF migration:
  - `20260306204450_ExpandOnboardingMilestonesPhase2`
  - Adds five non-null onboarding columns with `false` defaults for backward compatibility.
- Updated Family and Ledger microservice endpoint mappers/routes to stay contract-compatible.

## Desktop changes
- Expanded onboarding data service contract and payload mapping for all milestone fields.
- Updated onboarding wizard step list and deterministic first-incomplete-step routing for the Phase 2 sequence.
- Updated progress calculation to reflect all 8 milestones.

## Tests
- Updated API integration onboarding profile tests for new milestone payload/response behavior.
- Added desktop onboarding view model tests for:
  - first-incomplete step routing and progress percentage
  - mark-complete step advancement behavior

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj`
