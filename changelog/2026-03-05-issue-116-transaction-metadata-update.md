# Changelog - 2026-03-05 - Issue #116

## Summary
Added transaction metadata/category update API endpoint with family authorization and integration coverage.

## Changes
- Added contract:
  - `src/DragonEnvelopes.Contracts/Transactions/UpdateTransactionRequest.cs`
- Added domain update behavior:
  - `Transaction.UpdateMetadata(description, merchant, category)`
- Extended transaction repository interfaces/implementation:
  - fetch transaction for update
  - resolve transaction family id
  - save tracked changes
- Extended transaction application service:
  - `UpdateAsync(transactionId, description, merchant, category)`
- Added API endpoint:
  - `PUT /api/v1/transactions/{transactionId}`
  - family membership authorization enforced via account family lookup
- Added request validation:
  - non-empty merchant/description, category max length
- Added integration tests:
  - cross-family update forbidden
  - same-family metadata update succeeds and returns updated payload

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`
- Result: build succeeded, integration tests passed (8/8).
