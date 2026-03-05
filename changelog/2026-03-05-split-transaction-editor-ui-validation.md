# 2026-03-05 - Split Transaction Editor UI with Total Validation

## Summary
Implemented the desktop transactions create workflow with split transaction support and client-side total-equals validation.

## Changes
- Added split editor state and commands to `TransactionsViewModel`.
- Added transaction draft fields and submission flow to create transactions from desktop UI.
- Added split total recalculation and hard validation before submit.
- Added envelope lookup for split row assignment.
- Added transaction service methods for loading active envelopes and posting create-transaction payloads with optional splits.
- Added split draft and envelope option view models.
- Reworked Transactions page template into a resize-friendly two-column layout:
  - Left: transaction grid with filters/sort.
  - Right: create transaction form with optional split editor.

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj` (pass)

## Notes
- Split validation enforces exact amount match to align with API validation rules.
- Split rows require envelope assignment before submit.
