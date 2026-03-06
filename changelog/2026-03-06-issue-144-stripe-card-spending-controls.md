# 2026-03-06 - Issue #144 - Stripe Card Spending Controls

## Summary
- Added per-card spending controls with daily limit, allowed merchant categories, and allowed merchant names.
- Added auditable change history for card controls.
- Added Stripe provider sync path for spending controls updates.
- Added enforcement evaluation endpoint/service behavior for card authorization checks.

## Backend Changes
- Domain:
  - Added `EnvelopePaymentCardControl` entity (persisted control state).
  - Added `EnvelopePaymentCardControlAudit` entity (auditable control change records).
- Infrastructure:
  - Added EF configurations for control and audit tables.
  - Added repository interface/implementation for controls + audit history.
  - Added migration `20260306163643_AddEnvelopePaymentCardControls`.
- Application:
  - Added `IEnvelopePaymentCardControlService` + `EnvelopePaymentCardControlService`.
  - Added DTOs for control details, audit details, and spend evaluation results.
  - Added control validation and conflict checks (duplicates/wildcard conflicts/empty payload).
  - Added deterministic enforcement evaluation behavior:
    - Merchant allowlist check
    - Category allowlist check
    - Daily limit cap check
- Provider Client:
  - Extended `IStripeGateway` and Stripe gateway with `UpdateCardSpendingControlsAsync`.

## API Changes
- Added endpoints:
  - `PUT /api/v1/families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/controls`
  - `GET /api/v1/families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/controls`
  - `GET /api/v1/families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/controls/audit`
  - `POST /api/v1/families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/controls/evaluate`
- Added contracts for control upsert/read, audit read, and spend evaluation.
- Added FluentValidation validators for new requests.

## Tests
- Added application tests:
  - `EnvelopePaymentCardControlServiceTests`
  - Covers create+audit flow, provider sync failure behavior, and enforcement denials.
- Added integration auth/isolation tests:
  - Forbid cross-family control upsert.
  - Forbid cross-family audit access.

## Validation Run
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --no-build`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --no-build`
