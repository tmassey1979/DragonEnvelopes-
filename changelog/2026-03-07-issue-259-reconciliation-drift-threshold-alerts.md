# Issue 259: Reconciliation Drift Threshold Alerts + Desktop Surfacing

## Summary
- Added per-family reconciliation drift threshold configuration in financial profile.
- Added threshold update API and wired desktop controls to manage it.
- Added provider activity timeline drift alert surfacing (coalesced) with detail drilldown.
- Added desktop unresolved-drift highlighting in the Plaid reconciliation grid.

## Backend Changes
- Domain/Infrastructure:
  - Extended `FamilyFinancialProfile` with `ReconciliationDriftThreshold` and domain validation.
  - Added EF mapping for threshold and migration `20260307114924_AddFamilyReconciliationDriftThreshold`.
- Application:
  - Extended `FamilyFinancialProfileDetails` and `IFinancialIntegrationService`.
  - Added `UpdateReconciliationDriftThresholdAsync(...)` implementation.
- Contracts/API:
  - Extended `FamilyFinancialStatusResponse` with `ReconciliationDriftThreshold`.
  - Added `UpdateReconciliationDriftThresholdRequest` contract.
  - Added `PUT /api/v1/families/{familyId}/financial/reconciliation-threshold` (Parent policy).
  - Added request validator for threshold updates.
  - Updated provider activity health to compute drift counts/sums against configured threshold.
  - Added provider activity timeline source `PlaidReconciliation` with coalesced drift alerts and event detail support.

## Desktop Changes
- Added threshold edit/save workflow to Financial Integrations view:
  - `ReconciliationDriftThresholdInput`
  - `SaveReconciliationDriftThresholdCommand`
  - summary text for active threshold.
- Extended reconciliation account VM with `IsDriftAlert`.
- Updated reconciliation grid UI to:
  - highlight rows with unresolved threshold-breaching drift,
  - show alert summary count,
  - expose `Alert` indicator column.
- Added timeline source filter option for `Reconciliation Alerts`.

## Tests
- API integration tests:
  - parent can update threshold
  - provider health applies threshold (suppresses within tolerance)
  - reconciliation timeline drift alerts are coalesced and detail endpoint returns expected data
- Desktop smoke tests:
  - saving threshold updates summary and drift alert highlighting

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --filter "FinancialIntegrationServiceTests"`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --filter "Parent_Can_Update_Reconciliation_Drift_Threshold|Provider_Activity_Health_Uses_Reconciliation_Threshold_For_Drift_Counts|Provider_Activity_Timeline_Coalesces_Reconciliation_Drift_Alerts_And_Suppresses_Within_Threshold"`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --filter "Provider_Activity|Financial_Status|reconciliation-threshold"`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --filter "FinancialIntegrationsViewModelSmokeTests"`
