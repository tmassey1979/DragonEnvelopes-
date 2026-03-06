# Issue #141 - Stripe Financial Account Per Envelope

## Summary
Implemented Stripe financial account linkage per envelope with persisted mapping, API endpoints, migration, and tests.

## Delivered
- Added domain model:
  - `EnvelopeFinancialAccount`
- Added persistence:
  - `envelope_financial_accounts` table + EF configuration
  - repository interface + implementation
  - EF migration `AddEnvelopeFinancialAccounts`
- Added application service:
  - `IEnvelopeFinancialAccountService`
  - `EnvelopeFinancialAccountService`
- Extended Stripe gateway abstraction:
  - `IStripeGateway.CreateFinancialAccountAsync(...)`
  - provider client implementation in `DragonEnvelopes.ProviderClients`
- Added API endpoints:
  - `POST /api/v1/families/{familyId}/envelopes/{envelopeId}/financial-accounts/stripe`
  - `GET /api/v1/families/{familyId}/envelopes/{envelopeId}/financial-account`
  - `GET /api/v1/families/{familyId}/financial-accounts`
- Added request contract + validator:
  - `CreateStripeEnvelopeFinancialAccountRequest`
- Added tests:
  - application service tests for link behavior
  - integration auth/isolation tests for financial account routes

## Validation
- `dotnet build DragonEnvelopes.sln -nologo`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`
