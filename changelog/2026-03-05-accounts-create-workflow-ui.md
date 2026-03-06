# 2026-03-05 - Accounts Create Workflow UI

## Summary
Implemented real account creation on the desktop Accounts page, replacing the scaffold placeholder behavior.

## Changes
- Extended accounts data service contract/implementation with account create API call.
- Added create-account form state and commands in `AccountsViewModel`.
- Added client-side validation for name, type, and opening balance.
- Updated Accounts page template with create form (name/type/opening balance) and create/reset actions.
- Added auto-refresh of account list after successful create.

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj -p:OutputPath=bin/TempVerify/` (pass)

## Notes
- Active family context is required and automatically used for `POST /accounts`.
- Type options align to backend constraints: Checking, Savings, Cash, Credit.
