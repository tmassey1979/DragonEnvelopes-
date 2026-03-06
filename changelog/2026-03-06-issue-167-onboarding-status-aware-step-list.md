# 2026-03-06 - Issue #167 Onboarding status-aware step list UI

## Summary
Upgraded onboarding steps panel from a plain title list to a status-aware view that shows completed/current/pending state per step.

## Desktop changes
- Added new view model:
  - `OnboardingStepItemViewModel`
  - fields: step index, title, `IsCompleted`, `IsCurrent`, derived `StatusLabel`
- Updated `OnboardingWizardViewModel`:
  - maintains `StepItems` collection
  - syncs step status whenever profile state/current step changes
  - marks Review step complete when all milestones are complete
- Updated onboarding XAML step panel:
  - binds to `StepItems`
  - renders title + status label
  - applies distinct visual state for completed/current/pending rows

## Tests
- Extended onboarding wizard tests to assert:
  - step item completion/current flags after load
  - step item transitions after mark-complete

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj`
