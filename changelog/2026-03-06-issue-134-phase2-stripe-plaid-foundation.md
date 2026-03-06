# Issue #134 - Phase 2 Backend Stripe + Plaid Integration

## Summary
Implemented Phase 2 backend foundation for Stripe/Plaid with vendor-abstraction support through a dedicated provider client project.

## Delivered
- Added dedicated provider client project:
  - `src/DragonEnvelopes.ProviderClients`
  - Contains Plaid/Stripe client implementations and DI registration extension.
- Added family financial profile domain and persistence:
  - `FamilyFinancialProfile` entity
  - `family_financial_profiles` migration
  - repository + EF configuration + DbSet
- Added application financial integration service:
  - status retrieval
  - Plaid link-token flow
  - Plaid public-token exchange persistence
  - Stripe customer/setup-intent flow
- Added contracts and API endpoints:
  - `GET /api/v1/families/{familyId}/financial/status`
  - `POST /api/v1/families/{familyId}/financial/plaid/link-token`
  - `POST /api/v1/families/{familyId}/financial/plaid/exchange-public-token`
  - `POST /api/v1/families/{familyId}/financial/stripe/setup-intent`
- Added request validators and appsettings placeholders for Plaid/Stripe keys.
- Added tests:
  - application service tests for financial flow
  - integration tests for financial status auth/isolation

## Validation
- `dotnet build DragonEnvelopes.sln -nologo`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`

## Phase 2 Planning
Created Phase 2 epic/feature issue set from `Codex/phase2codex.md`:
- `#135` through `#148`.
