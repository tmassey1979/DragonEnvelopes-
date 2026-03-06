# Issue #175 - Onboarding Plaid connect and transaction import step

## Date
- 2026-03-06

## Summary
- Added onboarding wizard Plaid step UI with native-link flow controls:
  - create link token
  - launch native Plaid Link
  - exchange public token
  - map Plaid account id to selected internal account
  - remove existing account links
  - trigger transaction sync and show counts summary
- Extended onboarding wizard behavior to load account/link state for the Plaid step and gate step completion until at least one account link exists.
- Added a desktop unit test for Plaid-step completion guard behavior.

## Validation
- `dotnet build DragonEnvelopes.sln -v minimal` (pass)
- `dotnet test DragonEnvelopes.sln -v minimal` (pass)

## Notes
- The onboarding route now wires Plaid integration services into the wizard to reuse existing financial integration APIs.
- Errors from token exchange/link/sync/unlink are surfaced through the wizard error/status state for retry handling.
