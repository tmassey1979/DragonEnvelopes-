# Plaid Balance Drift Workflow

## Trigger Sources
- Scheduled worker: `PlaidBalanceRefreshWorker` (every 30 minutes)
- Manual endpoint: `POST /api/v1/families/{familyId}/financial/plaid/refresh-balances`

## Refresh Steps
1. Load the family's Plaid connection (`FamilyFinancialProfile`).
2. Load mapped account links (`plaid_account_links`).
3. Pull provider balances from Plaid (`/accounts/balance/get`).
4. For each mapped account:
   - Compare internal account balance vs provider balance.
   - Compute drift = `provider - internal`.
   - Apply correction to internal balance.
   - Persist a snapshot row in `plaid_balance_snapshots`.

## Reconciliation Report
- Endpoint: `GET /api/v1/families/{familyId}/financial/plaid/reconciliation`
- Returns per-account values:
  - internal balance
  - provider balance
  - drift amount
  - drift flag (`isDrifted`)

## Drift Handling Policy (Current)
- Drift is auto-corrected during refresh for mapped accounts.
- Every refresh persists an audit snapshot for traceability.
- If a provider account has no local mapping, it is excluded and reported via counters/telemetry.

## Operational Follow-up
- Review logs for large/recurring drift patterns.
- Confirm account mappings for any unmapped Plaid account ids.
- Use reconciliation endpoint before and after manual imports if investigating discrepancies.
