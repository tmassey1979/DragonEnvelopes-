# Issue 254 - Stripe Failed Webhook Replay API and Desktop Action

## Summary
Implemented replay support for failed Stripe webhook timeline events, including parent-authorized API replay endpoint, status transition auditing, and desktop replay action wiring.

## Delivered
- Added replay response contract: `ReplayStripeWebhookEventResponse`.
- Extended provider timeline contract with `StripeWebhookEventId` so replayable Stripe timeline items can be targeted.
- Added parent-only replay API route:
  - `POST /api/v1/families/{familyId}/financial/provider-activity/timeline/stripe-webhooks/{eventId}/replay`
- Implemented replay orchestration in `StripeWebhookService`:
  - `ReplayFailedEventAsync(familyId, webhookEventId)`
  - Supports replay attempts for `Failed` and `ReplayFailed` statuses.
  - Applies status transitions:
    - success -> `Replayed`
    - exception -> `ReplayFailed`
  - Persists updated timestamps/error state and preserves family scoping.
- Added repository support for replay updates (`GetByIdForUpdateAsync`).
- Updated Stripe webhook domain entity for controlled replay-state updates (`MarkReplayResult`).
- Updated provider activity timeline mapping to emit `StripeWebhookEventId` for Stripe rows.
- Desktop integration updates:
  - Added data service method for Stripe timeline replay endpoint.
  - Expanded timeline item VM to carry Stripe webhook id and replay capability flags.
  - Extended replay command logic to handle both notification replay and Stripe replay.
  - Added Stripe event id column in provider timeline grid.
- Added test coverage:
  - API integration tests for successful own-family replay + forbidden cross-family replay.
  - Desktop smoke test for replaying failed Stripe timeline event.

## Validation
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --configuration Release --no-restore`
  - Passed: 106, Failed: 0
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --configuration Release --no-restore`
  - Passed: 85, Failed: 0
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release --no-restore`
  - Passed: 96, Failed: 0
