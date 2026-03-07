# Issue #193 - Dashboard Independent Empty States

## Delivered
- Added independent empty-state flags in `DashboardViewModel`:
  - `IsKpiEmpty`
  - `IsRecentTransactionsEmpty`
- Updated dashboard load behavior to maintain section-specific empty-state values across:
  - successful data loads
  - partially empty data (KPI present, no transactions)
  - cancellation/error paths
- Updated dashboard XAML template bindings:
  - KPI card now binds `IsEmpty` to `IsKpiEmpty`
  - Recent Transactions card now binds `IsEmpty` to `IsRecentTransactionsEmpty`
- Extended dashboard tests to validate independent section behavior and avoid ambiguous empty-state rendering.

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --filter "DashboardViewModelTests" -v minimal`
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
