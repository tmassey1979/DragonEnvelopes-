# Service Delivery Pipeline Runbook

This runbook describes CI/CD promotion and rollback guidance for split API services.

## Services Covered
- `DragonEnvelopes.Api` (gateway/aggregation service)
- `DragonEnvelopes.Family.Api`
- `DragonEnvelopes.Ledger.Api`
- `DragonEnvelopes.Financial.Api`

## CI/CD Stages
1. `build-test` (Windows): full solution restore, architecture checks, contract checks, build, test, desktop artifacts.
2. `linux-service-build` (matrix): per-service restore/build in parallel.
3. `split-integration-tests` (matrix): Family/Ledger/Financial integration suites in parallel.
4. `docker-smoke-build` (matrix): Dockerfile build validation per service.
5. `compose-runtime-smoke`: full multi-service startup and e2e smoke.
6. `observability-compose-smoke`: multi-service startup with Loki/Grafana profile.
7. `docker-publish-ghcr` (push to `main` only): publishes service images to GHCR.

## Image Promotion Model
Images are published to GHCR as:
- `ghcr.io/<owner>/dragonenvelopes-api:<sha>`
- `ghcr.io/<owner>/dragonenvelopes-family-api:<sha>`
- `ghcr.io/<owner>/dragonenvelopes-ledger-api:<sha>`
- `ghcr.io/<owner>/dragonenvelopes-financial-api:<sha>`
- Plus `:main` moving tags for each service.

Promotion recommendation:
1. Promote immutable `:<sha>` tags between environments.
2. Treat `:main` as convenience only (not release provenance).
3. Record per-service deployed tag in release notes/change ticket.

## Rollback Guidance Per Service
1. Identify last known good image tag (`:<sha>`) for the impacted service.
2. Redeploy only that service to previous tag.
3. Keep other services pinned unless cross-service contract incompatibility is confirmed.
4. Validate readiness and key smoke routes for the rolled back service.
5. Monitor event pipeline lag/retry/failure/DLQ metrics during rollback stabilization.
6. If rollback does not stabilize, escalate to incident commander and consider full-stack rollback.

## Service-Specific Health Checks
- API gateway: `GET /health/ready` on `:18088`
- Family API: `GET /health/ready` on `:18089`
- Ledger API: `GET /health/ready` on `:18090`
- Financial API: `GET /health/ready` on `:18091`

## Failure Ownership
- Family service regressions: Family domain owner + platform on-call.
- Ledger service regressions: Ledger domain owner + platform on-call.
- Financial service regressions: Financial domain owner + platform on-call.
- Gateway routing regressions: Platform team primary owner.

## Operational Notes
- CI gating is intended to fail fast on any single-service regression.
- Integration contract drift checks must pass before image publish.
- Keep rollback decisions coupled to observed customer impact and SLO breach windows.
