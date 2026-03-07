# Issue #233: Family Member Remove Confirmation + Undo Window

## Summary
- Added a two-step confirmation flow for family member removal in desktop Family workspace.
- Added a short undo window after removal with compensating restore behavior.
- Added desktop tests for confirmation and undo workflows.

## Delivered
- Family members view model:
  - Added remove-confirmation window (10 seconds) requiring second remove action.
  - Added undo window (10 seconds) after successful removal.
  - Added `UndoRemoveMemberCommand`.
  - Added removal status state (`MemberRemovalStatus`) and undo availability state (`CanUndoRemove`).
- Family members UI:
  - Added `Undo Remove` button bound to undo command.
  - Added removal status text in selected-member section.
- Tests:
  - Updated removal test to verify confirmation-first behavior.
  - Added undo-removal restore test.

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
- `dotnet build DragonEnvelopes.sln -v minimal`
