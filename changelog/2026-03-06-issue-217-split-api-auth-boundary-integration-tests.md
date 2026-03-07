# Issue #217 - Split API integration tests: enforce cross-family authorization boundaries

## Summary
Expanded Family API and Ledger API integration tests from smoke-only checks to include explicit same-family allow and cross-family forbid authorization boundary coverage.

## Delivered
- Updated Family API integration tests:
  - `tests/DragonEnvelopes.Family.Api.IntegrationTests/FamilyApiSmokeTests.cs`
  - Added test: authenticated user can access own family but is forbidden from other family.
  - Added deterministic in-memory DB seeding for families + family membership.
  - Updated test auth handler to provide role claims and configured auth policies for test scheme.
  - Fixed test DB configuration to use a stable in-memory DB name per factory instance.
- Updated Ledger API integration tests:
  - `tests/DragonEnvelopes.Ledger.Api.IntegrationTests/LedgerApiSmokeTests.cs`
  - Added test: authenticated user can list own family accounts but is forbidden from other family.
  - Added deterministic in-memory DB seeding for families + membership + account data.
  - Updated test auth handler and auth policy overrides for test scheme.
  - Fixed test DB configuration to use a stable in-memory DB name per factory instance.

## Validation
- `dotnet test tests/DragonEnvelopes.Family.Api.IntegrationTests/DragonEnvelopes.Family.Api.IntegrationTests.csproj -v minimal`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -v minimal`

All validation commands completed successfully.
