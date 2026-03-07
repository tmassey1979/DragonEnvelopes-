# Issue 224: Transactions date range filters

## Summary
- Added `FromDateFilter` and `ToDateFilter` (`yyyy-MM-dd`) to `TransactionsViewModel`.
- Extended transaction filtering pipeline to support inclusive date boundaries.
- Added strict date validation with user-facing `DateFilterErrorMessage`.
- Updated Transactions grid controls to include `From`/`To` date filter inputs and inline filter validation text.
- Added desktop tests for:
  - inclusive boundary filtering
  - invalid date format validation messaging

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal`
- `dotnet build DragonEnvelopes.sln -v minimal`
