# Issue #216 - CI observability smoke: validate Grafana/Loki compose profile startup

## Summary
Added a dedicated CI smoke job that starts the observability stack with microservices, verifies Loki/Grafana (and API services) readiness endpoints, captures diagnostics on failure, and always tears down containers.

## Delivered
- Updated workflow:
  - `.github/workflows/ci.yml`
- Added job:
  - `observability-compose-smoke`
- Job behavior:
  - starts compose with `--profile observability --profile microservices`
  - waits for readiness:
    - API `http://localhost:18088/health/ready`
    - Family API `http://localhost:18089/health/ready`
    - Ledger API `http://localhost:18090/health/ready`
    - Loki `http://localhost:3100/ready`
    - Grafana `http://localhost:3000/api/health`
  - emits `docker compose ps` and service logs on timeout/failure
  - always runs teardown with `down -v --remove-orphans`

## Validation
- `docker compose --profile observability --profile microservices config`

Validation command completed successfully.
