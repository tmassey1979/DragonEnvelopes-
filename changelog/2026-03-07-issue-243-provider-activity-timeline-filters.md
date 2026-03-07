# Issue 243 - Provider Activity Timeline Filters

## Summary
Added source and status filtering support for provider activity timeline across API and desktop UI.

## Delivered
- API timeline endpoint updates (`GET /api/v1/families/{familyId}/financial/provider-activity/timeline`):
  - Added optional query params:
    - `source` (`StripeWebhook` or `NotificationDispatch`)
    - `status` (case-insensitive exact match)
  - Added `400 BadRequest` response for invalid `source` values.
  - Applied family-scoped filtering before timeline merge.
- Desktop financial integration service:
  - Extended `GetProviderActivityTimelineAsync` contract with optional `sourceFilter` and `statusFilter`.
  - Added safe query-string composition and URL encoding.
- Desktop provider activity UI:
  - Added timeline filter controls:
    - source dropdown (`All Sources`, `Stripe Webhooks`, `Notification Dispatch`)
    - status text filter
    - apply filters action via refresh timeline command
  - Updated timeline load flow to pass selected filters to API.
  - Updated timeline summary text to include active filter context.
- Test coverage:
  - API integration tests for source+status filtered timeline results.
  - API integration test for invalid source filter (`400`).
  - Desktop fake service updated to honor and capture timeline filters.
  - Desktop viewmodel smoke test for source/status filter flow.
- Updated `Codex/TaskLifecycleChecklist.md` session log for `#243` closeout.

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --configuration Release --no-restore`
  - Passed: 77, Failed: 0
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --configuration Release --no-restore`
  - Passed: 94, Failed: 0
