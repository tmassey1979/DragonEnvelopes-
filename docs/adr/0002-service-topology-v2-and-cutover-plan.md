# ADR 0002: Service Topology v2 and Cutover Contracts

- Status: Accepted
- Date: 2026-03-07
- Owners: Platform + Backend
- Related Issues: #277, #279, #280, #281, #282, #283, #284, #285, #286, #288

## Context
ADR 0001 defined an initial extraction path (Family -> Ledger) while deferring planning/reporting split. The codebase now has additional service seams and active migration stories that require a fully explicit target topology and cutover contract map.

The next risk to remove is route/data ownership ambiguity during the remaining extraction and monolith retirement phases.

## Decision
Adopt Service Topology v2 as the target runtime shape. The gateway remains the single public ingress and routes all external API calls to domain-owned services. Monolith runtime fallback is temporary and removed at end-state.

### Target Services and Ownership
| Service | Bounded Context | Primary Owned Data | External Route Ownership |
|---|---|---|---|
| `family-api` | Family identity, members, invites, onboarding profile | `families`, `family_members`, `family_invites`, `onboarding_profiles` | `/families/*`, `/auth/*` |
| `ledger-api` | Accounts and transaction ledger | `accounts`, `transactions`, `transaction_splits`, `approval_*`, `spend_anomaly_*` | `/accounts`, `/transactions`, `/approvals/*`, `/spend-anomalies*`, `/scenarios/*` |
| `planning-api` | Budgeting/planning lifecycle | `envelopes`, `budgets`, `recurring_bills`, `recurring_bill_executions`, `envelope_goals` | `/envelopes*`, `/budgets*`, `/recurring-bills*`, `/envelope-goals*` |
| `automation-api` | Rule management and deterministic evaluation | `automation_rules`, `automation_execution_*` | `/automation/rules*` |
| `ingestion-api` | CSV/import ingestion pipelines | `import_*` | `/imports/transactions/*` |
| `reporting-api` | Read-optimized reporting projections | projection/read-model tables | `/reports/*` |
| `financial-api` | Plaid/Stripe/cards/notifications | `plaid_*`, `stripe_*`, `provider_activity_*`, `notifications_*` | `/families/{familyId}/financial*`, `/webhooks/*` |
| `api-gateway` | Routing/security edge | none | Public ingress `/api/v1/*` |

### External Route Compatibility
| Public Route Prefix | Current Gateway Target | End-State Gateway Target |
|---|---|---|
| `/api/v1/families*`, `/api/v1/auth*` | `family-api` | `family-api` |
| `/api/v1/accounts*`, `/api/v1/transactions*`, `/api/v1/approvals*`, `/api/v1/scenarios*` | `ledger-api` | `ledger-api` |
| `/api/v1/envelopes*`, `/api/v1/budgets*`, `/api/v1/recurring-bills*`, `/api/v1/envelope-goals*` | mixed (`ledger-api`/monolith fallback) | `planning-api` |
| `/api/v1/automation/rules*` | `ledger-api` | `automation-api` |
| `/api/v1/imports/transactions/*` | `ledger-api` | `ingestion-api` |
| `/api/v1/reports/*` | `ledger-api` | `reporting-api` |
| `/api/v1/families/{familyId}/financial*`, `/api/v1/webhooks/*` | `financial-api` | `financial-api` |

Compatibility rule: external route shapes stay stable. Service relocation is internal to gateway routing.

### Integration Contract Choices (Sync vs Async)
| Interaction | Contract Type | Reason |
|---|---|---|
| Family access authorization check from non-family services | Synchronous internal API | Required for request-time auth decisions |
| Command-side state changes within a single bounded context | Synchronous local transaction | Invariant safety |
| Cross-service side effects and projections | Async events via RabbitMQ | Decoupling and resiliency |
| Reporting read-model refresh | Async event projections | Scale reads without write coupling |
| Workflow orchestration (onboarding/approval) | Async saga + compensations | Long-running reliability |

## Rollout and Rollback Toggles
| Phase | Toggle/Control | Forward Action | Rollback Action |
|---|---|---|---|
| Planning extraction | Gateway route switch for planning prefixes | Route prefixes to `planning-api` | Route prefixes back to prior handler while preserving single writer |
| Reporting extraction | Gateway route switch for `/reports/*` | Route to `reporting-api` | Route to prior handler/read path |
| Automation extraction | Gateway route switch for `/automation/rules*` | Route to `automation-api` | Route back to prior handler |
| Ingestion extraction | Gateway route switch for import prefixes | Route to `ingestion-api` | Route back to prior handler |
| Monolith retirement | Disable monolith profile and fallback | Remove fallback route path | Temporarily re-enable fallback only during incident window |

Non-negotiable: avoid dual-writer states for the same domain data.

## Dependency Order
1. Service boundary/contract freeze (#279)
2. Data ownership split (#284)
3. Cross-service auth contract enforcement (#285)
4. Service extraction by domain (#280, #281, #282, #283)
5. Gateway hardening and fallback removal (#286)
6. Pipeline expansion (#287)
7. Monolith retirement (#288)

## Consequences
Positive:
- Explicit ownership boundaries for endpoints and data.
- Clear migration order with predictable rollback controls.

Tradeoffs:
- Additional operational complexity (routing + service fleet).
- Temporary duplication overhead during cutover windows.
