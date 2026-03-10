# Technical Guide

This guide is for engineers operating or extending DragonEnvelopes.

## 1. Architecture Overview

DragonEnvelopes uses a layered .NET architecture with shared contracts and a desktop client.

Core projects:
- `src/DragonEnvelopes.Domain`
- `src/DragonEnvelopes.Application`
- `src/DragonEnvelopes.Infrastructure`
- `src/DragonEnvelopes.Contracts`
- `src/DragonEnvelopes.ProviderClients`
- `src/DragonEnvelopes.Api`
- `src/DragonEnvelopes.Family.Api`
- `src/DragonEnvelopes.Ledger.Api`
- `client/DragonEnvelopes.Desktop`

Service boundary intent:
- `DragonEnvelopes.Family.Api`: family profile, member, and identity-adjacent routes.
- `DragonEnvelopes.Ledger.Api`: accounts, transactions, envelopes, budgets, and reporting.
- `DragonEnvelopes.Api`: gateway-style surface and cross-cutting financial integrations.

Target topology and cutover contracts:
- `docs/adr/0002-service-topology-v2-and-cutover-plan.md`
- `docs/architecture/event-catalog-v1.md` (event-driven contract source of truth)
- `docs/architecture/gateway-route-ownership.md` (gateway route ownership and downstream readiness probes)

## 2. Local Prerequisites

Required locally:
- .NET 8 SDK
- Docker Desktop (or compatible Docker Engine + Compose)
- PowerShell 7+ recommended on Windows

Optional:
- GitHub CLI (`gh`) for issue workflow automation

## 3. Local Environment Setup

1. Create local env file:

```powershell
Copy-Item .env.example .env
```

2. Start baseline stack:

```powershell
docker compose up -d --build
```

3. Start split service profile (Family/Ledger/Financial containers behind gateway):

```powershell
docker compose --profile microservices up -d --build
```

4. Start observability profile (Grafana/Loki/Promtail):

```powershell
$env:OBSERVABILITY_ENABLE_LOKI_SINK='true'
docker compose --profile observability --profile microservices up -d
```

## 4. Common Validation Commands

```powershell
dotnet restore
dotnet build DragonEnvelopes.sln
dotnet test DragonEnvelopes.sln
```

Targeted test examples:

```powershell
dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj
dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj
```

## 5. Runtime Endpoints (Default Local)

- API Gateway (single base URL, microservices profile): `http://localhost:18088`
- Monolith API (direct): `http://localhost:18092`
- Family API (microservices profile): `http://localhost:18089`
- Ledger API (microservices profile): `http://localhost:18090`
- Financial API (microservices profile): `http://localhost:18091`
- Keycloak: `http://localhost:18080`
- pgAdmin: `http://localhost:5050`
- Postgres: `localhost:5433`
- Grafana (observability profile): `http://localhost:3000`
- Loki (observability profile): `http://localhost:3100`

## 6. Authentication and Roles

Auth is handled through Keycloak (OIDC/JWT).

Expected role model:
- Parent
- Adult
- Teen
- Child

Policy examples:
- Parent-only for sensitive family/financial admin operations.
- AnyFamilyMember for standard in-family reads.

Desktop sign-in uses Authorization Code + PKCE with loopback callback.

## 7. Configuration Notes

High-impact variables:
- `ConnectionStrings__Default`
- `Authentication__Authority`
- `Authentication__Audience`
- `DRAGONENVELOPES_API_BASE_URL`
- `DRAGONENVELOPES_FAMILY_API_BASE_URL`
- `DRAGONENVELOPES_LEDGER_API_BASE_URL`
- `OBSERVABILITY_ENABLE_LOKI_SINK`
- `OBSERVABILITY_LOKI_URL`

Do not commit production secrets. Use `.env.example` for non-secret defaults and secret stores in CI/CD.

## 8. Operational Areas

Review these docs for advanced operations:
- `docs/operations/plaid-balance-drift-workflow.md`
- `docs/operations/event-pipeline-observability.md`
- `docs/operations/provider-secret-key-rotation.md`
- `docs/operations/service-delivery-pipeline.md`
- `docs/qa/desktop-financial-integrations-smoke-checklist.md`

## 9. Contribution Workflow (Repo Standard)

Expected task lifecycle:
1. Create issue.
2. Add issue-type labels:
   - feature work: `feature`, `task`
   - bug work: `bug`, `task`
3. Add `inprogress` label.
4. Start comment with scope.
5. Implement + validate.
6. Add changelog file under `changelog/`.
7. Commit and push.
8. Completion comment with validation + time spent.
9. Remove `inprogress` and close issue.

Story format for both feature and bug issues:
- `As <x>, I would like <y>, so that <z>.`
- Include acceptance criteria and developer notes.

Use `Codex/TaskLifecycleChecklist.md` as the process checklist.
