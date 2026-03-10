# Issue 297 - Financial CQRS Refactor (Explicit Command/Query Stacks)

## Summary
Refactored Financial endpoint flows to explicit command/query bus execution with dedicated Financial handlers, preserving existing contracts while removing direct endpoint coupling to mutation/query services.

## Delivered
- Added a new `Application.Cqrs.Financial` package with explicit command/query contracts and handlers for:
  - Plaid integration: link token, public token exchange, account-link CRUD, sync, balance refresh, reconciliation report.
  - Stripe and financial accounts: setup intent, envelope account provisioning, family/envelope account queries.
  - Cards and controls: virtual/physical issuance, freeze/unfreeze/cancel, issuance read/refresh, controls upsert/read/audit, spend evaluation.
  - Notifications and webhooks: stripe webhook processing/replay, notification preference read/write, failed-event listing, retry/replay.
  - Financial profile operations: status query, reconciliation threshold update, provider secret rewrap.
- Added read-only `IFamilyFinancialStatusQueryService` + `FamilyFinancialStatusQueryService` and moved status query handling to that read service.
- Updated Financial API endpoints to call `ICommandBus` / `IQueryBus` explicitly in:
  - `FinancialIntegrationEndpoints.Plaid.cs`
  - `FinancialIntegrationEndpoints.StripeAccounts.cs`
  - `FinancialIntegrationEndpoints.Cards.cs`
  - `FinancialIntegrationEndpoints.WebhooksAndNotifications.cs`
- Registered all new financial handlers + status query service in `Application.DependencyInjection`.

## Validation
- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -c Release --no-build` (139 passed)
- `dotnet test tests/DragonEnvelopes.Financial.Api.IntegrationTests/DragonEnvelopes.Financial.Api.IntegrationTests.csproj -c Release --no-build` (2 passed)
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -c Release --no-build` (113 passed)
- `dotnet test DragonEnvelopes.sln -c Release --no-build` (397 passed)

## Time Spent
- 1h 35m
