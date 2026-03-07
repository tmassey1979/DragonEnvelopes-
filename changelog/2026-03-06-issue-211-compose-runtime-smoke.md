# Issue #211 - Compose Runtime Smoke Checks

## Delivered
- Added `compose-runtime-smoke` job to `.github/workflows/ci.yml`:
  - Starts compose services for API, Family API, and Ledger API with dependencies.
  - Waits for readiness endpoints with retry/timeout diagnostics.
  - Tears down compose resources in an always-run cleanup step.
- Fixed API runtime startup bug discovered by smoke validation:
  - Updated `RecurringBillAutoPostWorker` to resolve `IRecurringAutoPostService` from an async scope, avoiding scoped-from-singleton DI failure.

## Validation
- Local compose runtime smoke:
  - `docker compose --profile microservices up -d --build api family-api ledger-api postgres keycloak`
  - readiness checks for `http://localhost:18088/health/ready`, `http://localhost:18089/health/ready`, `http://localhost:18090/health/ready`
  - `docker compose --profile microservices down -v --remove-orphans`
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
