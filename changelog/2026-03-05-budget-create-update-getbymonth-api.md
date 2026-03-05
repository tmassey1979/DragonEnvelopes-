# 2026-03-05 - budget create/update/get-by-month API slice

## Summary
- Added budget repository abstraction + EF implementation.
- Added budget application service for create/get-by-month/update workflows.
- Added API endpoints:
  - `POST /api/v1/budgets`
  - `GET /api/v1/budgets/{familyId}/{month}`
  - `PUT /api/v1/budgets/{budgetId}`
- Added budget service unit tests.
- Updated README endpoint docs.

## Files Changed
- src/DragonEnvelopes.Application/DTOs/BudgetDetails.cs
- src/DragonEnvelopes.Application/Interfaces/IBudgetRepository.cs
- src/DragonEnvelopes.Application/Services/IBudgetService.cs
- src/DragonEnvelopes.Application/Services/BudgetService.cs
- src/DragonEnvelopes.Application/DependencyInjection.cs
- src/DragonEnvelopes.Infrastructure/Repositories/BudgetRepository.cs
- src/DragonEnvelopes.Infrastructure/DependencyInjection.cs
- src/DragonEnvelopes.Api/Program.cs
- tests/DragonEnvelopes.Application.Tests/BudgetServiceTests.cs
- README.md

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release`
- `docker compose up -d --build api`
- End-to-end smoke: onboard -> auth token -> create budget -> get by month -> update budget
