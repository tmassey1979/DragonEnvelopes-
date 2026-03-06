# 2026-03-06 - Issue #157

## Summary
- Added provider operations timeline support across API and desktop UI.

## Delivered
- Added timeline contracts:
  - `ProviderTimelineEventResponse`
  - `ProviderActivityTimelineResponse`
- Added API endpoint:
  - `GET /api/v1/families/{familyId}/financial/provider-activity/timeline?take={n}`
  - Returns merged Stripe webhook + notification dispatch timeline, family-scoped and take-bounded.
  - Reuses trace correlation (`TraceId` payload + `X-Trace-Id` header).
- Added desktop data service API:
  - `GetProviderActivityTimelineAsync`
- Added desktop UI/view model support:
  - `ProviderTimelineEventItemViewModel`
  - timeline collection + summary properties
  - refresh command + panel button
  - timeline data grid in Financial Integrations provider health card
- Added integration tests for timeline route auth boundaries and success response shape.
- Updated desktop smoke fake/test data to include timeline events.

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --no-build`
