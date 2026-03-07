# 2026-03-07 - Issue #269 - Goal-Based Envelope Targets API

## Summary
- Added envelope goal domain model:
  - `EnvelopeGoal` with target amount, due date, status, timestamps.
- Added contracts under `DragonEnvelopes.Contracts.EnvelopeGoals`:
  - create/update requests
  - goal response
  - projection response
- Added application service and repository contracts:
  - `IEnvelopeGoalService` / `EnvelopeGoalService`
  - `IEnvelopeGoalRepository`
- Added infrastructure persistence:
  - EF configuration `EnvelopeGoalConfiguration`
  - repository `EnvelopeGoalRepository`
  - `DbSet<EnvelopeGoal>`
  - migration `AddEnvelopeGoals`
- Added ledger API endpoints:
  - `POST /api/v1/envelope-goals`
  - `GET /api/v1/envelope-goals?familyId=...`
  - `GET /api/v1/envelope-goals/{goalId}`
  - `PUT /api/v1/envelope-goals/{goalId}`
  - `DELETE /api/v1/envelope-goals/{goalId}`
  - `GET /api/v1/envelope-goals/projection?familyId=...&asOf=...`
- Added projection logic with deterministic `OnTrack` / `Behind` status.
- Added FluentValidation rules for create/update goal requests.
- Added tests:
  - application service tests (`EnvelopeGoalServiceTests`)
  - ledger integration auth/isolation coverage for goals + projection

## Validation
- `dotnet build DragonEnvelopes.sln -c Release --nologo`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -c Release --nologo`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -c Release --nologo`

## Notes
- Envelope goals are optional and enforced as one-goal-per-envelope.
- Projection is read-only and uses decimal math with rounded progress/variance values.
