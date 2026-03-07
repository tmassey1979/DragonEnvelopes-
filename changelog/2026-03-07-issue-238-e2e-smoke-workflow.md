# Issue 238 - End-to-End Smoke Workflow in CI

## Summary
Added a scripted CI smoke scenario that validates the core authenticated flow end-to-end: login/session restore, family reads, transaction import, and reporting read.

## Delivered
- Added `eng/run-e2e-smoke.sh`:
  - Onboards a new family via `POST /api/v1/families/onboard`.
  - Authenticates against Keycloak using password grant.
  - Verifies session via `GET /api/v1/auth/me`.
  - Restores session using refresh token grant and verifies `GET /api/v1/auth/me` again.
  - Performs family operations:
    - `GET /api/v1/families/{familyId}`
    - `GET /api/v1/families/{familyId}/members`
  - Creates account via `POST /api/v1/accounts`.
  - Runs transaction import flow:
    - `POST /api/v1/imports/transactions/preview`
    - `POST /api/v1/imports/transactions/commit`
  - Reads reporting endpoint:
    - `GET /api/v1/reports/envelope-balances?familyId=...`
  - Emits per-request response artifacts and writes `artifacts/e2e-smoke/e2e-smoke-summary.md`.
  - Fails fast with actionable error messages when any step status is unexpected.
- Updated `.github/workflows/ci.yml` (`compose-runtime-smoke` job):
  - Added step to execute `bash ./eng/run-e2e-smoke.sh` after service readiness.
  - Added failure-only compose log capture into `artifacts/e2e-smoke`.
  - Added always-on upload of `e2e-smoke-artifacts`.
- Updated `Codex/TaskLifecycleChecklist.md` active task log to issue `#238`.

## Validation
- `bash -n eng/run-e2e-smoke.sh`
- Local compose smoke run:
  - `docker compose --profile microservices up -d --build api family-api ledger-api postgres keycloak`
  - readiness checks for API/Family API/Ledger API
  - `bash ./eng/run-e2e-smoke.sh` (pass)
  - `docker compose --profile microservices down -v --remove-orphans`
