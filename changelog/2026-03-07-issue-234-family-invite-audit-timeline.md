# Issue 234 - Family Invite Audit Timeline

## Summary
Implemented a full invite lifecycle timeline across API + desktop for create/resend/cancel/accept/redeem events.

## Delivered
- Added domain entity + enum for invite timeline events:
  - `FamilyInviteTimelineEvent`
  - `FamilyInviteTimelineEventType`
- Added persistence and migration:
  - EF config for `family_invite_timeline_events`
  - DbContext `DbSet`
  - migration `20260307064713_AddFamilyInviteTimelineEvents`
- Extended invite repository/service contracts and implementations to:
  - record timeline events for create/resend/cancel/accept/redeem
  - query timeline by family with `email`, `eventType`, and bounded `take`
- Added timeline API endpoint in both hosts:
  - `GET /api/v1/families/{familyId}/invites/timeline`
- Added contract and endpoint mappers:
  - `FamilyInviteTimelineEventResponse`
- Desktop family workspace updates:
  - timeline model `FamilyInviteTimelineItemViewModel`
  - data service fetch for timeline endpoint
  - email/event filter state + summary in `FamilyMembersViewModel`
  - new timeline section in `ShellTemplates.xaml` with filter controls and datagrid
- Tests:
  - API integration tests for timeline filter behavior and cross-family access denial
  - Family API integration smoke for timeline route + isolation
  - Desktop viewmodel test for timeline filtering logic

## Validation
- `dotnet build DragonEnvelopes.sln -v minimal`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal --no-build`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -v minimal --no-build --filter "FullyQualifiedName~Invite_Timeline"`
- `dotnet test tests/DragonEnvelopes.Family.Api.IntegrationTests/DragonEnvelopes.Family.Api.IntegrationTests.csproj -v minimal --no-build --filter "FullyQualifiedName~Invite_Timeline"`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -v minimal --no-build`
- `dotnet test tests/DragonEnvelopes.Family.Api.IntegrationTests/DragonEnvelopes.Family.Api.IntegrationTests.csproj -v minimal --no-build`
