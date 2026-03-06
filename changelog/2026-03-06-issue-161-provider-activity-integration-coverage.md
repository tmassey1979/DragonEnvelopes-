# 2026-03-06 - Issue #161 Provider activity auth/isolation integration coverage

## Summary
Expanded API integration coverage for provider activity health/timeline endpoints with family-scoped seeded activity to verify cross-family isolation behavior.

## Test enhancements
- Seeded Stripe webhook activity for both family A and family B with distinct event markers.
- Strengthened provider health assertions to verify family A only sees its own webhook marker and degraded notification state.
- Strengthened provider timeline assertions to verify family A includes family A webhook activity and excludes family B webhook markers.
- Retained existing forbidden-path coverage for family B requests by user A.

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
