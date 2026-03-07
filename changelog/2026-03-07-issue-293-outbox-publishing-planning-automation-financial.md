# Issue 293 - Outbox Publishing for Planning, Automation, and Financial Services

## Summary
Implemented transactional outbox publishing for Planning, Automation, and Financial write paths, expanded event contracts/routing, and added service workers/config updates so Ledger and Financial APIs can dispatch multi-source outbox messages through RabbitMQ.

## Delivered
- Added shared domain event contracts + names for Planning, Automation, and Financial integrations:
  - `DomainIntegrationEvents.cs`
  - `DomainIntegrationEventNames.cs`
  - routing key additions in `LedgerTransactionCreatedIntegrationEvent.cs`
- Added reusable outbox helper:
  - `IntegrationOutboxEnqueuer` for standardized event-id/correlation/timestamp serialization.
- Added Planning outbox emission on write flows:
  - `EnvelopeService`, `BudgetService`, `RecurringBillService`, `EnvelopeGoalService`.
- Added Automation outbox emission:
  - `AutomationRuleService` lifecycle events (create/update/enable/disable/delete).
  - `TransactionService` execution events (`AutomationRuleExecuted`) for categorization/allocation automation paths.
- Added Financial outbox emission:
  - `EnvelopeFinancialAccountService` (`StripeFinancialAccountProvisioned`).
  - `EnvelopePaymentCardService` (virtual/physical/frozen/unfrozen/cancelled card events).
  - `SpendNotificationDispatchService` (dispatch failed/retried events).
- Updated repository save semantics to support atomic aggregate + outbox persistence:
  - envelope, budget, recurring bill, envelope goal, automation rule, financial account/card/shipment repositories now defer `SaveChanges` to service unit-of-work.
- Added provider-aware case-insensitive check fallback:
  - `EnvelopeRepository.EnvelopeNameExistsAsync` now uses PostgreSQL `ILike` only on Npgsql and a safe fallback for non-Npgsql tests.
- Expanded outbox workers:
  - Ledger worker now dispatches across configured source services (`ledger-api`, `planning-api`, `automation-api`).
  - Added Financial outbox worker + options and wiring in Financial API bootstrap/settings.
- Added/updated tests:
  - Application unit tests across Planning/Automation/Financial services for outbox message emission.
  - `TransactionServiceTests` now cover automation execution outbox events.
  - Ledger API integration smoke coverage for planning/automation source dispatch behavior.
  - Stabilized a provider timeline integration test timestamp to prevent time-window flakiness.

## Validation
- `dotnet build src/DragonEnvelopes.Application/DragonEnvelopes.Application.csproj -c Release`
- `dotnet build src/DragonEnvelopes.Infrastructure/DragonEnvelopes.Infrastructure.csproj -c Release`
- `dotnet build src/DragonEnvelopes.Ledger.Api/DragonEnvelopes.Ledger.Api.csproj -c Release`
- `dotnet build src/DragonEnvelopes.Financial.Api/DragonEnvelopes.Financial.Api.csproj -c Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -c Release`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -c Release`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -c Release`
- `dotnet test tests/DragonEnvelopes.Family.Api.IntegrationTests/DragonEnvelopes.Family.Api.IntegrationTests.csproj -c Release`

## Time Spent
- 3h 30m
