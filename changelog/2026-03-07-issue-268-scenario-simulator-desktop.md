# 2026-03-07 - Issue #268 - Scenario Simulator Desktop Workspace

## Summary
- Added desktop scenario simulation workspace with:
  - Assumption inputs (income, expenses, discretionary cut %, month horizon)
  - Run simulation action
  - Month-by-month chart and table output
  - Loading/error/empty states
  - CSV export action
- Added new desktop route:
  - `/scenarios`
- Added API data service integration:
  - `POST scenarios/simulate` via ledger client
- Added CSV exporter service for simulation outputs.
- Added view model and routing tests for scenario workspace.

## Tests
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -c Release --nologo`

## Notes
- Scenario route intentionally uses ledger client to match backend API ownership.
- Export path defaults to `%LocalAppData%/DragonEnvelopes/Exports`.
