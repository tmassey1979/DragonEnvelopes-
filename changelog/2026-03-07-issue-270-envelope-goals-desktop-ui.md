# 2026-03-07 - Issue #270 - Goal-Based Envelope Targets Desktop UI

## Summary
- Added envelope-goal UI support directly in desktop envelope workflow:
  - Create/edit/delete goal behavior integrated with envelope save flows.
  - Goal target, due date, and status editor controls.
  - Goal toggle to enable/disable tracking per envelope.
- Added goal progress and due-date indicators in envelope list rows.
- Added dashboard goal summary KPI card:
  - total goals
  - on-track count
  - behind count
- Added goal-capable envelope data service integration:
  - calls goal CRUD endpoints
  - hydrates list rows from goal + projection APIs
- Added desktop tests for envelope goal state transitions and updated dashboard-related tests.

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -c Release --nologo`

## Notes
- Goal projection status is surfaced as `OnTrack` / `Behind`.
- Due-date badge states shown as `On Schedule`, `Due Soon`, or `Overdue`.
