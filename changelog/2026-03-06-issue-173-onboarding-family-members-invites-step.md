# 2026-03-06 - Issue #173 Onboarding family members invite step

## Summary
Added a dedicated onboarding family-members step with API-backed invite creation/list/cancel flows and milestone completion driven by real member/invite state.

## Desktop changes
- Extended onboarding wizard with `IFamilyMembersDataService` integration.
- Added Family Members step UI:
  - invite form (`email`, `role`, `expiresInHours`)
  - members list
  - invites list with status
  - cancel-selected-invite action
- Added onboarding commands:
  - `CreateInviteCommand`
  - `CancelInviteCommand`
- Added deterministic step visibility for onboarding content panels:
  - family profile
  - family members/invites
  - accounts
  - envelopes
  - budget
- Members milestone completion now syncs from real state:
  - complete when member count >= 2, or pending/accepted invites exist
  - persisted through onboarding profile update API

## Supporting changes
- Updated route registry to construct onboarding view model with family-members service.
- Updated family-bound refresh pipeline to reload onboarding view model on family selection changes.

## Tests
- Added `FakeFamilyMembersDataService` for desktop onboarding tests.
- Updated onboarding wizard tests for constructor/signature changes.
- Added invite flow test to verify real-state completion sync behavior.

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test DragonEnvelopes.sln`
