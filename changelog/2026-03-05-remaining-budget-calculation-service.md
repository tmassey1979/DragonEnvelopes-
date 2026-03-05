# 2026-03-05 - remaining budget calculation service and edge-case tests

## Summary
- Added reusable `RemainingBudgetCalculator` service in application layer.
- Calculator behavior:
  - Uses decimal arithmetic only.
  - Clamps negative income and negative allocation inputs to zero.
  - Supports over-budget outputs by returning negative remaining values.
- Integrated calculator into `BudgetService` so API budget responses expose computed `AllocatedAmount` and `RemainingAmount` using active family envelope monthly budgets.
- Excludes archived envelopes from allocated monthly total.
- Added edge-case tests for zero, over-budget, and negative-input scenarios.

## Files Changed
- src/DragonEnvelopes.Application/DTOs/RemainingBudgetDetails.cs
- src/DragonEnvelopes.Application/Services/IRemainingBudgetCalculator.cs
- src/DragonEnvelopes.Application/Services/RemainingBudgetCalculator.cs
- src/DragonEnvelopes.Application/Services/BudgetService.cs
- src/DragonEnvelopes.Application/DependencyInjection.cs
- tests/DragonEnvelopes.Application.Tests/RemainingBudgetCalculatorTests.cs
- tests/DragonEnvelopes.Application.Tests/BudgetServiceTests.cs

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release`
- `docker compose up -d --build api`
- End-to-end smoke: create envelopes + create budget -> API response includes computed allocated/remaining values
