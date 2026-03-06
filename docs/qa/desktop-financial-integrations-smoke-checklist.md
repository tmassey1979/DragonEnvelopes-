# Desktop Financial Integrations Smoke Checklist

Use this checklist when automated desktop smoke tests are unavailable or when validating release candidates manually.

## Preconditions
- Backend API is running and reachable from desktop client.
- Test parent user can sign in and select an active family.
- Family has at least one account and one envelope.

## Smoke Path
1. Open desktop app and navigate to `Integrations`.
2. Verify route loads without errors:
   - Status card renders.
   - Plaid card renders.
   - Stripe and Card Management card renders.
   - Card Controls and Evaluation card renders.
3. Click `Refresh Status`:
   - No error banner appears.
   - `Status` card updates `Updated` timestamp.
4. Plaid smoke:
   - Click `Create Link Token` and verify token/expires fields update.
   - Click `Launch Native Plaid Link` and complete/cancel flow.
   - Click `Sync` and verify sync summary text updates.
   - Click `Refresh Balances` and verify balance refresh summary updates.
   - Click `Reconciliation` and verify reconciliation table rows load.
5. Stripe setup smoke:
   - Enter valid email/name and click `Create Setup Intent`.
   - Verify setup intent id, customer id, and client secret fields update.
6. Card action smoke:
   - Select envelope and verify cards table loads.
   - Run `Freeze`, `Unfreeze`, and `Cancel` against selected card.
   - Verify status summary updates after each action.
7. Controls and evaluation smoke:
   - Set at least one control value and click `Save Controls`.
   - Verify audit rows appear/refresh.
   - Enter spend evaluation input and click `Evaluate Spend`.
   - Verify allow/deny result text updates.

## Failure Triage
- Capture operation text from top status/subheader (for example `Refreshing Plaid balances... failed.`).
- Capture error banner message text.
- Attach screenshot and timestamp.
- Include active family id and selected envelope/card ids in bug report.
