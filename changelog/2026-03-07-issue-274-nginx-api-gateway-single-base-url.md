# 2026-03-07 - Issue #274 - NGINX API Gateway for Single Base URL

## Summary
- Added an NGINX API gateway that exposes one API ingress (`API_PORT`, default `18088`) and routes `/api/v1/*` paths to Family, Ledger, and Financial services.
- Added fallback behavior so routed requests fall back to monolith API when a split service is unavailable.
- Moved direct monolith host binding to `MONOLITH_API_PORT` (default `18092`) to keep the gateway port stable for desktop clients.

## Files Changed
- `infrastructure/nginx/api-gateway.conf`
- `docker-compose.yml`
- `.env.example`
- `README.md`
- `docs/technical-guide.md`

## Routing Notes
- Family routes: `/api/v*/families*`, `/api/v*/auth*`
- Ledger routes: `/api/v*/accounts*`, `/transactions*`, `/envelopes*`, `/budgets*`, `/reports*`, `/automation*`, `/approvals*`, `/recurring-bills*`, `/imports*`, `/spend-anomalies*`, `/envelope-goals*`, `/scenarios*`
- Financial routes: `/api/v*/families/{id}/financial*`, `/financial-accounts*`, `/notifications*`, card endpoints under envelope routes, and `/api/v*/webhooks/{stripe|plaid}`
- Any split-service 502/503/504 falls back to monolith API.

## Validation
- `docker run --rm -v ${PWD}/infrastructure/nginx/api-gateway.conf:/etc/nginx/nginx.conf:ro nginx:1.27-alpine nginx -t`
- `docker compose config -q`
- `docker compose up -d api api-gateway`
- `Invoke-WebRequest http://localhost:18088/health/ready`
- `Invoke-WebRequest http://localhost:18092/health/ready`
- `Invoke-WebRequest http://localhost:18088/api/v1/system/health`
- Fallback spot check by stopping split services and verifying gateway request status consistency.
