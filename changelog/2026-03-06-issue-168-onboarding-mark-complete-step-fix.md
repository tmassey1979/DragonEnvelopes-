# 2026-03-06 - Issue #168 Onboarding mark-complete double-advance fix

## Summary
Fixed onboarding wizard progression so `Mark Complete` advances to the immediate next incomplete milestone instead of skipping ahead.

## Desktop changes
- Removed manual `CurrentStepIndex++` in `MarkCurrentStepCompleteAsync`.
- Progression now relies on profile-driven `DetermineCurrentStepIndex` synchronization only.
- Updated onboarding tests to assert exact next-step behavior (no skip) and corresponding step-item current state.

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj`
