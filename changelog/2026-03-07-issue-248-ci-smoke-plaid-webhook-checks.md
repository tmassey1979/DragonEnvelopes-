# Issue 248 - CI Smoke Plaid Webhook Checks

## Summary
Extended the e2e smoke script to cover Plaid webhook endpoint behavior, matching existing Stripe webhook failure-path coverage.

## Delivered
- Updated `eng/run-e2e-smoke.sh` with Plaid webhook checks:
  - malformed JSON payload to `POST /api/v1/webhooks/plaid` asserts `400`.
  - unknown item payload to `POST /api/v1/webhooks/plaid` asserts `200`.
  - validates unknown-item webhook response `outcome == Ignored`.
- Added Plaid webhook status fields to generated smoke summary:
  - invalid-json status
  - unknown-item status
- Kept existing Stripe webhook invalid-signature smoke coverage unchanged.
- Updated `Codex/TaskLifecycleChecklist.md` active task log for `#248`.

## Validation
- `bash -n eng/run-e2e-smoke.sh`
  - Passed (no syntax errors)
