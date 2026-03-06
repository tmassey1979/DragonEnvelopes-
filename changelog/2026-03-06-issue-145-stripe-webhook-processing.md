# 2026-03-06 - Issue #145 - Stripe Webhook Processing

## Summary
- Added Stripe webhook endpoint and processing pipeline for `card_authorization`, `card_transaction`, and `balance_update` events.
- Added signature verification and idempotency persistence.
- Added transactional envelope/card updates with auditable webhook event records.

## Backend Changes
- Domain:
  - Added `StripeWebhookEvent` entity for auditable webhook processing records.
- Infrastructure:
  - Added EF config + migration for `stripe_webhook_events`.
  - Added `IStripeWebhookEventRepository` + repository implementation.
  - Extended card repository with provider-card lookup methods for webhook mapping.
- Application:
  - Added `IStripeWebhookService` + `StripeWebhookService`.
  - Added webhook options/result models.
  - Implemented:
    - Stripe signature verification (`Stripe-Signature` HMAC validation + timestamp tolerance)
    - Idempotency by unique Stripe event id
    - Event processing handlers for:
      - `card_authorization` (envelope spend/credit handling)
      - `card_transaction` (envelope spend/credit handling)
      - `balance_update` (card status refresh)
    - Failure capture to webhook event table (`ProcessingStatus=Failed`) as dead-letter record.
- API:
  - Added anonymous endpoint:
    - `POST /api/v1/webhooks/stripe`
  - Added response contract:
    - `StripeWebhookProcessResponse`
  - Added Stripe webhook options binding in API startup.
- Configuration:
  - Added `Stripe:Webhooks` section in API `appsettings.json` and `appsettings.Development.json`.

## Tests
- Added `StripeWebhookServiceTests` (application tests):
  - invalid signature handling
  - duplicate event idempotency
  - successful card transaction balance update + audit record
  - failed spend path captured as failed webhook event
- Added integration test:
  - invalid Stripe signature returns `401`.

## Validation Run
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --no-build`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
