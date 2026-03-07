# Issue 255 - Provider Timeline Event Detail Drilldown with Payload Redaction

## Summary
Added provider timeline event detail drilldown APIs and desktop UI to inspect Stripe/Plaid/notification timeline events with redacted payload previews and family-scoped access control.

## Delivered
- Added timeline detail API endpoint:
  - `GET /api/v1/families/{familyId}/financial/provider-activity/timeline/events/{source}/{eventId}`
  - Supports `source` values: `StripeWebhook`, `PlaidWebhook`, `NotificationDispatch`.
- Added detail contract:
  - `ProviderTimelineEventDetailResponse`
- Extended timeline event contract with source-specific ids:
  - `StripeWebhookEventId`
  - `PlaidWebhookEventId`
- Updated provider timeline API mapping to populate source-specific event ids.
- Added payload redaction/truncation pipeline for webhook detail responses:
  - key-based sensitive field masking (`secret`, `token`, `password`, `authorization`, etc.)
  - bounded payload preview length with truncation indicator.
- Desktop updates:
  - added provider timeline detail data service method
  - added timeline detail command and viewmodel properties
  - added detail panel UI in provider activity workspace
  - added plaid event id column in timeline grid
  - replay button label generalized to timeline events.
- Added tests:
  - API integration tests for own-family detail retrieval and cross-family forbidden behavior
  - API assertion for redacted payload content
  - desktop smoke test for loading selected timeline detail payload.

## Validation
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --configuration Release --no-restore`
  - Passed: 108, Failed: 0
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --configuration Release --no-restore`
  - Passed: 86, Failed: 0
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release --no-restore`
  - Passed: 96, Failed: 0
