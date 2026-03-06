# 2026-03-06 - Issue #159 Manual notification retry operations

## Summary
Implemented manual retry support for failed spend-notification dispatch events with family-scoped API endpoints and desktop UI actions.

## Backend
- Added failed dispatch event contracts and retry response contracts.
- Extended notification dispatch repository with:
  - failed event listing by family
  - family+id fetch for update
- Extended dispatch service with:
  - `ListFailedEventsAsync(familyId, take)`
  - `RetryFailedEventAsync(familyId, eventId)`
- Added API endpoints:
  - `GET /api/v1/families/{familyId}/notifications/dispatch-events/failed?take={n}`
  - `POST /api/v1/families/{familyId}/notifications/dispatch-events/{eventId}/retry`

## Desktop
- Added data-service methods to list failed dispatch events and retry a selected event.
- Added failed-dispatch event view-model item and selection state.
- Added UI controls in the provider activity panel:
  - refresh failed events
  - retry selected failed notification
  - failed event grid with channel/merchant/amount/attempt/error

## Tests
- Application tests for failed-event listing and manual retry service behavior.
- API integration tests for:
  - listing failed dispatch events (authorized family)
  - retry success for own family failed event
  - retry forbidden for cross-family request
- Desktop smoke coverage for retry selected failed event command.

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
