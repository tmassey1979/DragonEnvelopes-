# 2026-03-05 - Auth Boundaries and Cross-Family Isolation Integration Tests (#83)

## Summary
- Added route-level family-scope authorization checks in API endpoints using authenticated user `sub` -> `family_members` membership.
- Enforced scope checks on read and write paths for:
  - accounts
  - transactions
  - imports
  - envelopes
  - budgets
  - automation rules
  - recurring bills
  - report endpoints
  - family get/member list
- Added stricter list behavior for leakage-prone routes:
  - `GET /api/v1/accounts` now requires `familyId`
  - `GET /api/v1/transactions` now requires `accountId`
- Updated startup migration behavior to support integration testing with non-relational providers:
  - run `Migrate()` for relational DBs
  - run `EnsureCreated()` for non-relational providers
- Added API integration test project with test auth + seeded Family A/B isolation fixture.

## Integration Tests Added
- `DragonEnvelopes.Api.IntegrationTests` project.
- `AuthIsolationIntegrationTests` covering:
  - unauthorized request -> `401`
  - user A denied listing family B accounts -> `403`
  - user A denied creating transaction against family B account -> `403`
  - reports denied for cross-family access -> `403`, allowed for caller family -> `200`

## Validation
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --configuration Release`
- `docker compose up -d --build api`
- Authenticated live API smoke confirmed cross-family `403` behavior on accounts/transactions/reports.
