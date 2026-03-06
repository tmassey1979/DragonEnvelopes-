# Changelog - 2026-03-05 - Issue #117

## Summary
Implemented transaction allocation replacement with envelope rebalance support, including split replacement in update flow.

## Changes
- Extended update contract:
  - `UpdateTransactionRequest` now supports `ReplaceAllocation`, `EnvelopeId`, and `Splits`.
- Extended transaction update service to support allocation replacement:
  - reverse prior envelope impact (single-envelope or split)
  - apply new envelope/split allocation impact
  - replace persisted split rows for the transaction
- Added repository capabilities:
  - list splits by transaction id
  - replace transaction split rows
- Updated API endpoint behavior:
  - `PUT /api/v1/transactions/{transactionId}` now accepts allocation replacement payload
- Updated validation:
  - allocation replacement envelope/split exclusivity and split item validation
- Added integration coverage:
  - same-family update with allocation replacement to splits
  - verifies envelope balances rebalance and split rows are replaced

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`
- Result: build succeeded; integration tests passed (9/9).
