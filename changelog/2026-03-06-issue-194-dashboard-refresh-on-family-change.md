# Issue #194 - Refresh Dashboard On Family Context Change

## Delivered
- Updated `MainWindowViewModel.RefreshFamilyBoundViewModelsAsync` to include `DashboardViewModel` in the family-bound refresh loop.
- Added desktop unit coverage in `MainWindowViewModelTests`:
  - invokes the family-bound refresh method
  - verifies dashboard reload path executes (`DashboardViewModel.LoadCommand`)
- Ensures dashboard KPI/activity data is refreshed consistently when family context changes.

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --filter "MainWindowViewModelTests" -v minimal`
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
