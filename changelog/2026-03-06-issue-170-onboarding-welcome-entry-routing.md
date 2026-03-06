# 2026-03-06 - Issue #170 Onboarding welcome entry routing

## Summary
Added a welcome entry experience to login onboarding with explicit `Get Started` and `Sign In` routes.

## Desktop changes
- Added login entry routing state model:
  - `LoginEntryState`
  - `LoginWelcomeAction`
  - `LoginEntryRoute`
- Updated `LoginControl` UI:
  - new welcome panel
  - welcome actions: `Get Started`, `Sign In`, `Cancel`
  - sign-in panel with `Back`, `Cancel`, `Sign In`
- Updated `LoginControl` behavior:
  - `GetStartedRequested` event replaces create-family event
  - deterministic route transitions between welcome and sign-in states
  - `ShowSignInView()` / `ShowWelcomeView()` helpers
- Updated `LoginWindow` wiring:
  - handles `Get Started` by launching create-family account flow
  - after successful family creation, routes user to sign-in view and pre-fills created email

## Tests
- Added `LoginEntryStateTests` to verify routing behavior:
  - welcome `Sign In` routes to sign-in form and toggles state
  - welcome `Get Started` routes to create-family path
  - `ShowWelcomeView` resets sign-in visibility

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj`
