# Issue 252 - Plaid Webhook Authenticity Verification Gate

## Summary
Added Plaid webhook signature verification with fail-closed behavior, explicit bypass configuration for local development, and persisted verification failures for provider activity visibility.

## Delivered
- Added `IPlaidWebhookVerificationService` contract and verification option/result models:
  - `PlaidWebhookVerificationOptions`
  - `PlaidWebhookVerificationResult`
- Added provider-client implementation `PlaidWebhookVerificationService` with:
  - Stripe-style signed payload verification (`t=<timestamp>,v1=<hmac>`) via `Plaid-Signature` header
  - tolerance-window timestamp validation
  - fixed-time signature comparison
  - explicit unsigned bypass switch (`AllowUnsignedInDevelopment`)
- Registered webhook verification service and options in provider-client DI.
- Updated Plaid webhook API endpoint to:
  - verify authenticity before payload processing,
  - reject unauthorized webhook calls when verification fails,
  - persist verification failures as `PlaidWebhookEvent` (`Failed`, `WebhookType=Unknown`) for provider timeline visibility.
- Added `Plaid:Webhooks` configuration section in API appsettings:
  - strict-by-default verification in base config,
  - explicit development bypass enabled in `appsettings.Development.json`.
- Extended integration tests with verification coverage:
  - unsigned webhook rejected + persisted failure event
  - valid signed webhook accepted
  - default test host keeps verification disabled unless explicitly enabled per test.

## Validation
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --configuration Release --no-restore`
  - Passed: 102, Failed: 0
