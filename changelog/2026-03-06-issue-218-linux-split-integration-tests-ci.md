# Issue #218 - CI hardening: run split integration test suites on Linux

## Summary
Extended Linux CI to run split API integration tests so family-authorization and ledger-isolation regressions are gated cross-platform.

## Delivered
- Updated `.github/workflows/ci.yml`:
  - In `linux-service-build` job, added `Run split integration tests` step.
  - Executes:
    - `tests/DragonEnvelopes.Family.Api.IntegrationTests`
    - `tests/DragonEnvelopes.Ledger.Api.IntegrationTests`
  - Uses `--configuration Release --verbosity minimal` for both commands.

## Validation
- `dotnet test tests/DragonEnvelopes.Family.Api.IntegrationTests/DragonEnvelopes.Family.Api.IntegrationTests.csproj --configuration Release --verbosity minimal`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj --configuration Release --verbosity minimal`

All validation commands completed successfully.
