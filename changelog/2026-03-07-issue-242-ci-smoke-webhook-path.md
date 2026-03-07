# Issue 242 - CI Smoke Webhook Path Coverage

## Summary
Extended the end-to-end CI smoke script to validate Stripe webhook endpoint behavior in a deterministic invalid-signature scenario.

## Delivered
- Updated `eng/run-e2e-smoke.sh`:
  - Added Stripe webhook smoke step:
    - `POST /api/v1/webhooks/stripe` with synthetic payload and intentionally invalid `Stripe-Signature`.
    - Asserts expected `401` failure-path behavior.
  - Added result output into smoke summary:
    - `Stripe webhook invalid-signature status`.
  - Added reusable `wait_for_ready` helper.
  - Hardened Keycloak readiness check by waiting on realm OIDC discovery endpoint:
    - `/realms/{realm}/.well-known/openid-configuration`
    - avoids false waits on unavailable `/health/ready` endpoint in current local setup.
- Updated `Codex/TaskLifecycleChecklist.md` active run log for issue `#242`.

## Validation
- `bash -n eng/run-e2e-smoke.sh`
- Local compose smoke run:
  - `docker compose --profile microservices up -d --build api family-api ledger-api postgres keycloak`
  - readiness checks for API/family/ledger
  - `bash ./eng/run-e2e-smoke.sh` (pass, includes webhook `401` assertion)
  - `docker compose --profile microservices down -v --remove-orphans`
