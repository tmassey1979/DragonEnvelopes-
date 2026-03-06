# 2026-03-06 - Issue #143 - Stripe Physical Card Issuing Workflow

## Summary
- Added physical card issuing flow with shipping details.
- Added persisted issuance/shipping status model and query/refresh APIs.
- Added authorization boundary tests for cross-family physical card routes.

## Backend Changes
- Domain:
  - Added `EnvelopePaymentCardShipment` for persisted shipping/issuance state.
- Infrastructure:
  - Added `envelope_payment_card_shipments` EF config + migration.
  - Added shipment repository interface/implementation.
- Application:
  - Extended `IEnvelopePaymentCardService` with:
    - `IssuePhysicalCardAsync`
    - `GetPhysicalCardIssuanceAsync`
    - `RefreshPhysicalCardIssuanceStatusAsync`
  - Extended `EnvelopePaymentCardService` implementation for physical flow.
  - Added shipment DTOs and issuance aggregate DTO.
- Provider Clients:
  - Extended Stripe gateway contract + implementation with:
    - `CreatePhysicalCardAsync`
    - `GetCardStatusAsync`
  - Added issuing model records for shipping + status payloads.

## API Changes
- Added contracts for physical issue request + issuance/shipment responses.
- Added routes:
  - `POST /api/v1/families/{familyId}/envelopes/{envelopeId}/cards/physical`
  - `GET /api/v1/families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/issuance`
  - `POST /api/v1/families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/issuance/refresh`
- Added request validator for physical card issuing request.

## Tests
- Added/updated application tests for physical issue + status refresh behavior.
- Added integration auth/isolation tests:
  - cross-family physical issue forbidden
  - cross-family issuance lookup forbidden

## Validation Run
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --no-build`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --no-build`
