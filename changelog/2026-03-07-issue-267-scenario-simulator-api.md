# 2026-03-07 - Issue #267 - Scenario Simulator API v1

## Summary
- Added scenario simulation contracts under `DragonEnvelopes.Contracts.Scenarios`.
- Implemented deterministic simulation service in application layer:
  - `IScenarioSimulationService`
  - `ScenarioSimulationService`
- Added new ledger endpoint:
  - `POST /api/v1/scenarios/simulate`
- Enforced family authorization boundary on scenario simulation endpoint.
- Added FluentValidation rules for `SimulateScenarioRequest`.
- Added mapper conversions for scenario simulation responses.

## Tests
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -c Release --nologo`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -c Release --nologo`

## Notes
- Simulation performs no writes and uses decimal-only calculations.
- Rule: projection is deterministic month-by-month from current family account balance baseline.
