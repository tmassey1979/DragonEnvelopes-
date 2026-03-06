# ADR 0001: Microservice Boundaries and Migration Plan

- Status: Accepted
- Date: 2026-03-06
- Owners: Platform + Backend
- Related Issues: #129, #130, #131, #132
- Baseline Commit: `3579f2f` (endpoint modularization in monolith)

## Context
The API was previously centralized in a single startup file and single deployable. Endpoint modularization is complete and now provides bounded seams (`src/DragonEnvelopes.Api/Endpoints/*`) for extraction.

Current risk drivers:
- Single deployable couples identity/family flows with ledger write throughput.
- Shared database ownership across all domains increases migration risk.
- Reporting queries span transactional and planning data and can become a bottleneck.

## Decision
Adopt a phased microservice extraction from the current modular monolith with the following initial service boundaries:

1. `family-service` (first extraction)
2. `ledger-service` (second extraction)
3. Keep planning/reporting in monolith boundary temporarily, then reassess split after ledger stabilization.

Extraction order is fixed for now: `Family/Identity` -> `Ledger`.

## Bounded Context and Ownership Matrix
| Domain Capability | Owning Service | Primary Data | External API Ownership |
|---|---|---|---|
| Family lifecycle, members, invites, onboarding profile/bootstrap | `family-service` | `families`, `family_members`, `family_invites`, `onboarding_profiles` | `/families/*`, `/families/invites/accept` |
| Accounts, transactions, transaction splits, import preview/commit, automation rules | `ledger-service` | `accounts`, `transactions`, `transaction_splits`, `automation_rules` | `/accounts`, `/transactions`, `/imports/transactions/*`, `/automation/rules/*` |
| Envelopes, budgets, recurring bills, recurring bill execution | monolith (phase hold) -> candidate `planning-service` | `envelopes`, `budgets`, `recurring_bills`, `recurring_bill_executions` | `/envelopes`, `/budgets`, `/recurring-bills/*` |
| Reporting projections | monolith (phase hold) -> candidate `reporting-service` | read models over planning + ledger data | `/reports/*` |

## Data Ownership Strategy
- Each extracted service owns its own datastore and migrations.
- Service-local migrations must run with service startup/deploy pipeline; no shared migration project after extraction.
- Family service data must be removed from monolith migration ownership once cutover is complete.
- Ledger service data must be removed from monolith migration ownership once cutover is complete.

Initial datastore split target:
- `dragonenvelopes_family` database for family service tables.
- `dragonenvelopes_ledger` database for ledger tables.

## API and Integration Contracts
External API stability rule:
- Existing public route shapes remain stable during migration.
- Routing compatibility is provided by gateway/proxy during cutover.

Required internal contracts (v1):
- Family access check for downstream authorization (internal):
  - `GET /internal/v1/family-access/{familyId}/users/{keycloakUserId}` -> `{ hasAccess: bool, roles: string[] }`
- Family identity events (async, for downstream consumers):
  - `FamilyCreated`
  - `FamilyMemberAdded`
  - `FamilyMemberRemoved`
  - `FamilyInviteAccepted`

Eventing requirement:
- Event publishing is introduced after synchronous extraction path is stable.
- During phase 1, synchronous HTTP contract is acceptable for authorization checks.

## Authentication and Authorization Model
- JWT validation stays at edge/API gateway and each service validates token issuer/audience.
- Family-scoped authorization source of truth is `family-service`.
- Any service that handles `familyId` resources must enforce family membership using either:
  - signed internal claim from gateway populated from family-service check, or
  - direct call to family-service access endpoint (with short cache).

Non-negotiable rule:
- No service may authorize family resource access solely from client-supplied `familyId`.

## Migration Plan
### Phase 0 (completed)
- Endpoint modularization in monolith (`3579f2f`).

### Phase 1 (this ADR)
- Finalize bounded contexts, contracts, rollout/rollback gates.

### Phase 2 (`#130`) Family extraction
- Create `family-service` deployable.
- Move family endpoints and family-owned tables/migrations to family service.
- Add gateway/proxy routing for family routes.
- Validate auth isolation parity with integration tests.

### Phase 3 (`#131`) Ledger extraction
- Create `ledger-service` deployable.
- Move ledger endpoints and ledger-owned tables/migrations.
- Integrate family-access authorization check from family service.
- Validate transaction/import/automation parity.

### Phase 4 (post-131)
- Re-evaluate planning and reporting split based on latency/load and ownership friction.

## Rollout Gates
A phase can move to next only when all are true:
- Build and tests green for owning service and compatibility surface.
- Cross-family isolation tests pass against routed deployment.
- Error rate and p95 latency are within 10% of baseline for 48 hours.
- Runbook and rollback route toggle are verified in staging.

## Rollback Strategy
- Keep compatibility routing switchable by configuration.
- Retain old route handlers until new service passes gate window.
- Rollback path: route traffic back to monolith handlers, keep data writes in one place only.

## Consequences
Positive:
- Clear ownership boundaries and deploy velocity improvements.
- Reduced blast radius for family vs ledger changes.

Tradeoffs:
- Cross-service auth and consistency become explicit operational concerns.
- Requires gateway routing and service-level observability maturity.

## Implementation Notes
- Source seams already exist in:
  - `src/DragonEnvelopes.Api/Endpoints/FamilyEndpoints.cs`
  - `src/DragonEnvelopes.Api/Endpoints/AccountAndTransactionEndpoints.cs`
  - `src/DragonEnvelopes.Api/Endpoints/PlanningAndReportingEndpoints.cs`
- This ADR intentionally defers planning/reporting extraction to avoid over-splitting before family+ledger stabilize.
