# Gateway Route Ownership

This table documents API gateway route ownership for the split service topology.

## Principles
- Gateway routes are owned by domain services (`Family`, `Ledger`, `Financial`).
- Unmapped `/api/*` routes return `404` (no monolith fallback path).
- Desktop client base URL remains a single gateway ingress (`/api/v1`).

## Route Map

| Route Pattern | Owner Service | Notes |
| --- | --- | --- |
| `/api/v*/families/*/financial*` | `financial-api` | Family-scoped financial integration endpoints. |
| `/api/v*/families/*/financial-accounts*` | `financial-api` | Financial account link and lifecycle endpoints. |
| `/api/v*/families/*/notifications*` | `financial-api` | Notification dispatch and timeline actions. |
| `/api/v*/families/*/envelopes/*/cards*` | `financial-api` | Card issuance/control endpoints. |
| `/api/v*/families/*/envelopes/*/financial-account*` | `financial-api` | Envelope financial account wiring. |
| `/api/v*/webhooks/stripe*` | `financial-api` | Stripe webhook ingress. |
| `/api/v*/webhooks/plaid*` | `financial-api` | Plaid webhook ingress. |
| `/api/v*/(accounts|transactions|envelopes|budgets|reports|automation|recurring-bills|imports|approvals|spend-anomalies|envelope-goals|scenarios)*` | `ledger-api` | Ledger + planning + reporting bounded context routes currently hosted in Ledger API. |
| `/api/v*/families/*/recurring-bills/*` | `ledger-api` | Family-scoped recurring bill route variant. |
| `/api/v*/(families|auth)*` | `family-api` | Family profile, onboarding, members, auth/session endpoints. |

## Gateway Readiness Endpoints
- `/health/ready`: gateway process ready.
- `/health/ready/family`: family downstream readiness proxy.
- `/health/ready/ledger`: ledger downstream readiness proxy.
- `/health/ready/financial`: financial downstream readiness proxy.

## CI Validation
Route ownership is validated by:
- `eng/run-gateway-route-smoke.sh` (representative Family/Ledger/Financial routes)
- compose smoke workflow readiness checks for gateway downstream probes.
