# Issue 257: Envelope-to-Envelope Transfer Workflow

## Summary
- Added a dedicated envelope transfer workflow that creates linked debit/credit transaction legs under a shared transfer id.
- Enforced transfer validation for same-family envelopes, non-identical source/target envelopes, positive amount, and sufficient funds.
- Added desktop transaction workspace support to initiate transfers without creating manual fake spend/income entries.

## Backend Changes
- Domain:
  - Extended `Transaction` with transfer metadata (`TransferId`, `TransferCounterpartyEnvelopeId`, `TransferDirection`, `IsTransfer`) and invariants.
- Application:
  - Added `IEnvelopeTransferService` and `EnvelopeTransferService` for atomic transfer creation.
  - Added `EnvelopeTransferDetails` DTO and transfer request/response contracts.
  - Updated `TransactionService` to map transfer metadata and block edit/delete/restore operations on transfer legs.
  - Updated reporting aggregation to exclude transfer legs from spend/category totals.
- Infrastructure:
  - Extended transaction EF configuration for transfer columns/index/FK.
  - Added migration `20260307110108_AddEnvelopeTransferTransactionMetadata`.
  - Updated reporting repository projection to carry transfer id metadata.
- API / Ledger / Family mapping:
  - Added `POST /api/v1/transactions/envelope-transfers` in both monolith API and ledger API.
  - Added validators for transfer request payloads.
  - Updated endpoint mappers to surface transfer metadata in transaction responses.

## Desktop Changes
- Added transfer creation call to desktop transactions data service.
- Extended transactions view model with transfer draft state and submit command.
- Added transfer panel in transactions workspace (from envelope, to envelope, amount, note).
- Extended transaction list rows with transfer metadata display.
- Updated capability matrix to include envelope transfer support.

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --filter "EnvelopeTransferServiceTests|ReportingServiceTests"`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --filter "TransactionsViewModelTests|TransactionListItemViewModelTests"`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj --filter "EnvelopeTransfer"`
