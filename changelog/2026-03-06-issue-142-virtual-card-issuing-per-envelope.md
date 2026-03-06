# Issue #142 - Virtual Debit Card Issuing Per Envelope

## Summary
Implemented virtual card issuing support per envelope with persistence, service orchestration, Stripe client abstraction methods, API endpoints, migration, and tests.

## Delivered
- Added domain model:
  - `EnvelopePaymentCard`
- Added persistence:
  - `envelope_payment_cards` table + EF configuration
  - repository interface + implementation
  - migration `AddEnvelopePaymentCards`
- Added application service:
  - `IEnvelopePaymentCardService`
  - `EnvelopePaymentCardService`
- Extended Stripe gateway abstraction:
  - `CreateVirtualCardAsync(...)`
  - `UpdateCardStatusAsync(...)`
- Added API endpoints:
  - `POST /api/v1/families/{familyId}/envelopes/{envelopeId}/cards/virtual`
  - `GET /api/v1/families/{familyId}/envelopes/{envelopeId}/cards`
  - `POST /api/v1/families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/freeze`
  - `POST /api/v1/families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/unfreeze`
  - `POST /api/v1/families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/cancel`
- Added request contract + validator:
  - `CreateVirtualEnvelopeCardRequest`
- Added tests:
  - application service tests for card issuance and freeze flows
  - integration auth/isolation tests for card routes

## Validation
- `dotnet build DragonEnvelopes.sln -nologo`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`
