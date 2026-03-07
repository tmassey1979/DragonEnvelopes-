# Issue 220: Desktop transactions edit/save parity

## Summary
- Added desktop transaction update support via `PUT /api/v1/transactions/{transactionId}`.
- Extended transaction list models to carry allocation metadata (`EnvelopeId` and split snapshots) so existing transactions can be edited safely.
- Added Transactions workspace edit mode with:
  - select row + load into editor
  - create/edit mode labels and submit button text
  - cancel edit flow
  - allocation replace toggle for edits (`ReplaceAllocation=false` by default)
- Kept create flow intact.
- Added desktop unit tests for edit submit behavior:
  - preserve allocation update payload
  - split replacement payload

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal`
- `dotnet build DragonEnvelopes.sln -v minimal`

## Notes
- In edit mode, amount/date are read-only because the current API update contract supports metadata/allocation updates, not amount/date changes.
