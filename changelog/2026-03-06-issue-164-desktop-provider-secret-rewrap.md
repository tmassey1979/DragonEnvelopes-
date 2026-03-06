# 2026-03-06 - Issue #164 Desktop provider secret rewrap operation

## Summary
Added desktop support for running provider secret rewrap from Financial Integrations, including command wiring, UI trigger, and status summary output.

## Desktop changes
- Added data service method:
  - `RewrapProviderSecretsAsync()`
- Added view model command:
  - `RewrapProviderSecretsCommand`
- Added summary binding:
  - `ProviderSecretRewrapSummary`
- Added UI trigger button in provider activity operations:
  - `Rewrap Secrets`
- Rewrap action now updates summary text and refreshes integration status.

## Tests
- Extended desktop smoke tests to validate:
  - command execution
  - call count increment
  - summary/status update behavior

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj`
