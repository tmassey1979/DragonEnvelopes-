# Issue 296 - Planning CQRS Refactor (Explicit Command/Query Stacks)

## Summary
Refactored Planning endpoint flows (envelopes, budgets/rollover, recurring bills, envelope goals) to explicit command/query bus execution with dedicated handlers, preserving existing API contracts and behavior.

## Delivered
- Added Planning CQRS contracts + handlers in `Application.Cqrs.Planning`:
  - Envelopes:
    - `CreateEnvelopeCommand`
    - `GetEnvelopeByIdQuery`
    - `ListEnvelopesByFamilyQuery`
    - `UpdateEnvelopeCommand`
    - `UpdateEnvelopeRolloverPolicyCommand`
    - `ArchiveEnvelopeCommand`
  - Budgets/Rollover:
    - `CreateBudgetCommand`
    - `GetBudgetByMonthQuery`
    - `UpdateBudgetCommand`
    - `PreviewEnvelopeRolloverQuery`
    - `ApplyEnvelopeRolloverCommand`
  - Recurring Bills:
    - `CreateRecurringBillCommand`
    - `ListRecurringBillsByFamilyQuery`
    - `UpdateRecurringBillCommand`
    - `DeleteRecurringBillCommand`
    - `ProjectRecurringBillsQuery`
    - `ListRecurringBillExecutionsQuery`
  - Envelope Goals:
    - `CreateEnvelopeGoalCommand`
    - `ListEnvelopeGoalsByFamilyQuery`
    - `GetEnvelopeGoalByIdQuery`
    - `UpdateEnvelopeGoalCommand`
    - `DeleteEnvelopeGoalCommand`
    - `ProjectEnvelopeGoalsQuery`
- Updated Planning endpoints to call `ICommandBus` / `IQueryBus` explicitly:
  - `PlanningEndpoints.Envelopes.cs`
  - `PlanningEndpoints.Budgets.cs`
  - `PlanningEndpoints.RecurringBills.cs`
  - `PlanningEndpoints.EnvelopeGoals.cs`
- Registered all new planning handlers in `Application.DependencyInjection`.

## Validation
- `dotnet build src/DragonEnvelopes.Ledger.Api/DragonEnvelopes.Ledger.Api.csproj -c Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -c Release` (139 passed)
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -c Release` (24 passed)

## Time Spent
- 1h 00m
