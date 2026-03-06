# Changelog - 2026-03-06 - Issue #127

## Summary
Completed onboarding quality coverage with additional integration guardrail test and a desktop onboarding smoke checklist.

## Changes
- Added API integration test:
  - bootstrap rejects duplicate account names in request (`422 UnprocessableEntity`)
- Added desktop runbook artifact:
  - `docs/onboarding-smoke-checklist.md`
  - happy path, resume path, negative path, and isolation checks

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj -nologo`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -nologo`
- Result: build succeeded; integration tests passed (18/18).
