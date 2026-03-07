# Issue #188 - Timeline Replay for Failed Notification Dispatch

## Delivered
- Added timeline replay API endpoint for spend notification dispatch events:
  - `POST /api/v1/families/{familyId}/financial/provider-activity/timeline/notifications/{eventId}/replay`
- Reused notification dispatch service path with idempotent replay behavior:
  - added `ReplayEventAsync(...)` on `ISpendNotificationDispatchService`.
  - replay returns existing `Sent` event as-is (idempotent) instead of failing.
  - existing strict retry endpoint remains available.
- Extended provider timeline API payload to include replay target id:
  - `ProviderTimelineEventResponse.NotificationDispatchEventId`
- Added desktop timeline replay UX:
  - timeline row selection bound to view model
  - replay command for selected failed notification timeline event
  - replay eligibility surfaced per row (`Replayable` marker)
- Updated desktop replay status reporting:
  - retry/replay summary now includes status transition, attempt count, timestamp, and last error.

## Tests
- Application tests:
  - `ReplayEventAsync_IsIdempotent_ForAlreadySentEvent`
- API integration tests:
  - timeline includes replayable notification event ids
  - own-family timeline replay succeeds and is idempotent
  - cross-family timeline replay is forbidden
- Desktop smoke tests:
  - replay selected timeline failed notification transitions state and updates summary/status

## Validation
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --filter "NotificationServicesTests" -v minimal`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --filter "Replay_Own_Failed_Notification_Dispatch_Event_From_Timeline|Replay_FamilyB_Notification_Dispatch_Event_From_Timeline|Provider_Activity_Timeline" -v minimal`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --filter "FinancialIntegrationsViewModelSmokeTests" -v minimal`
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
