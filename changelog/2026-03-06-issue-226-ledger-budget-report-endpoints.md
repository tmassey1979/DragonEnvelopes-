# Issue 226: Ledger API budget/report endpoint parity

## Summary
- Added Ledger API budget endpoint mappings:
  - `POST /api/v1/budgets`
  - `GET /api/v1/budgets/{familyId}/{month}`
  - `PUT /api/v1/budgets/{budgetId}`
- Added Ledger API reporting endpoint mappings:
  - `GET /api/v1/reports/envelope-balances`
  - `GET /api/v1/reports/monthly-spend`
  - `GET /api/v1/reports/category-breakdown`
  - `GET /api/v1/reports/remaining-budget`
- Updated planning endpoint group registration to include envelope + budget + report mapping modules.
- Expanded Ledger integration tests for:
  - own/other family budget access boundaries
  - own/other family envelope balance report boundaries

## Validation
- `dotnet build DragonEnvelopes.sln -v minimal`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -v minimal`

## Notes
- An initial parallel test/build execution hit a transient file lock in `obj/bin`; sequential rerun passed cleanly.
