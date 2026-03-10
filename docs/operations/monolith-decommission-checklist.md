# Monolith Decommission Checklist

This checklist governs retirement of `DragonEnvelopes.Api` runtime traffic path.

## Objective
Serve production/runtime traffic through split services (`family-api`, `ledger-api`, `financial-api`) behind the gateway, with monolith runtime disabled by default.

## Pre-Decommission Checks
1. Verify gateway route ownership map is current:
   - `docs/architecture/gateway-route-ownership.md`
2. Verify gateway route smoke checks pass:
   - `eng/run-gateway-route-smoke.sh`
3. Verify end-to-end smoke passes through gateway without monolith profile enabled:
   - `eng/run-e2e-smoke.sh`
4. Verify split-service compose smoke and observability smoke are green in CI.
5. Confirm desktop client base URL points to gateway (`DRAGONENVELOPES_API_BASE_URL` -> `:18088`).

## Decommission Actions
1. Keep monolith container behind `legacy-monolith` profile only.
2. Keep gateway routing microservice-only (no monolith fallback handlers).
3. Keep route ownership docs and CI smoke checks as release gates.
4. Mark monolith runtime as deprecated in ops/docs and release notes.

## Rollback Strategy
If split-service incident impact requires rollback:
1. Start legacy monolith profile explicitly:
   - `docker compose --profile legacy-monolith up -d --build api`
2. Temporarily direct clients to monolith base URL (`:18092`) only under incident change control.
3. Capture incident timeline and root cause while rollback is active.
4. Restore gateway split-service route path after mitigation and re-validation.
5. Re-run route smoke and e2e smoke before closing rollback window.

## Exit Criteria
- All gateway traffic paths validated against split services.
- No required client workflow depends on monolith-only handlers.
- CI/CD smoke gates remain green with monolith profile disabled.
- Rollback playbook tested and documented for controlled emergency use.
