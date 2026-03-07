# Issue #187 - Recurring Execution Filtering and CSV Export

## Delivered
- Extended recurring execution history API query options:
  - `result`
  - `fromDate`
  - `toDate`
- Updated recurring execution application service filtering:
  - result filtering (case-insensitive)
  - execution timestamp date range filtering (UTC date)
  - invalid range guard (`fromDate > toDate`) returns domain validation error.
- Updated desktop recurring execution history UX:
  - filter controls for result, from date, to date
  - filtered refresh behavior on execution history list.
- Added desktop CSV export for currently filtered execution rows.
  - CSV includes `IdempotencyKey`, `Result`, `ExecutedAtUtc` (ISO-8601), and `Notes` plus additional context columns.
  - export file path: `%LOCALAPPDATA%/DragonEnvelopes/Exports/...`.
- Added reusable desktop CSV exporter service:
  - `IRecurringExecutionCsvExporter`
  - `RecurringExecutionCsvExporter`

## Tests
- Application tests:
  - `ListExecutionsAsync_FiltersByResult_AndExecutionDateRange`
  - `ListExecutionsAsync_ThrowsWhenDateRangeIsInvalid`
- API integration test:
  - `UserA_Can_Filter_Own_RecurringBill_Executions_By_Result_And_DateRange`
- Desktop tests:
  - `RecurringExecutionCsvExporterTests` for required columns, ISO timestamp, and CSV escaping.

## Validation
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --filter "RecurringBillServiceTests" -v minimal`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --filter "RecurringBill_Executions|Filter_Own_RecurringBill_Executions" -v minimal`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --filter "RecurringExecutionCsvExporterTests" -v minimal`
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
