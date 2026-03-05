# 2026-03-05 - reporting query handlers and report endpoints

## Summary
- Added reporting application service and repository for read-optimized report queries.
- Implemented reports for:
  - Envelope balances
  - Monthly spend
  - Category breakdown
  - Remaining budget
- Added API endpoints:
  - `GET /api/v1/reports/envelope-balances?familyId={familyId}`
  - `GET /api/v1/reports/monthly-spend?familyId={familyId}&from={isoDate}&to={isoDate}`
  - `GET /api/v1/reports/category-breakdown?familyId={familyId}&from={isoDate}&to={isoDate}`
  - `GET /api/v1/reports/remaining-budget?familyId={familyId}&month={yyyy-MM}`
- Added reporting service unit tests for monthly spend and category breakdown aggregation.
- Added report response contracts and README endpoint docs.

## Files Changed
- src/DragonEnvelopes.Contracts/Reports/EnvelopeBalanceReportResponse.cs
- src/DragonEnvelopes.Contracts/Reports/MonthlySpendReportPointResponse.cs
- src/DragonEnvelopes.Contracts/Reports/CategoryBreakdownReportItemResponse.cs
- src/DragonEnvelopes.Contracts/Reports/RemainingBudgetReportResponse.cs
- src/DragonEnvelopes.Application/DTOs/ReportDetails.cs
- src/DragonEnvelopes.Application/Interfaces/IReportingRepository.cs
- src/DragonEnvelopes.Application/Services/IReportingService.cs
- src/DragonEnvelopes.Application/Services/ReportingService.cs
- src/DragonEnvelopes.Application/DependencyInjection.cs
- src/DragonEnvelopes.Infrastructure/Repositories/ReportingRepository.cs
- src/DragonEnvelopes.Infrastructure/DependencyInjection.cs
- src/DragonEnvelopes.Api/Program.cs
- tests/DragonEnvelopes.Application.Tests/ReportingServiceTests.cs
- README.md

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release`
- `docker compose up -d --build api`
- End-to-end smoke: report endpoints returned expected monthly spend, category totals, envelope balances, and remaining budget values
