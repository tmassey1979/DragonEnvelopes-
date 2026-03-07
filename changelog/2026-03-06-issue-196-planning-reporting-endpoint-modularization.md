# Issue #196 - Planning and Reporting Endpoint Modularization

## Delivered
- Refactored `PlanningAndReportingEndpoints` into partial endpoint mapping files by concern:
  - `PlanningAndReportingEndpoints.Envelopes.cs`
  - `PlanningAndReportingEndpoints.Budgets.cs`
  - `PlanningAndReportingEndpoints.RecurringBills.cs`
  - `PlanningAndReportingEndpoints.Reports.cs`
- Simplified `PlanningAndReportingEndpoints.cs` to orchestration only.
- Kept public registration unchanged (`MapPlanningAndReportingEndpoints`).
- Preserved route behavior, auth policies, and operation names.
- Fixed namespace import for `IRecurringAutoPostService` after refactor split.

## Validation
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
