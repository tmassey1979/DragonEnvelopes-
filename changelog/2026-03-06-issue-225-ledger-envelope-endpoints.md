# Issue 225: Ledger API envelope endpoint parity

## Summary
- Added envelope planning endpoints to `DragonEnvelopes.Ledger.Api`:
  - `POST /api/v1/envelopes`
  - `GET /api/v1/envelopes/{envelopeId}`
  - `GET /api/v1/envelopes?familyId=...`
  - `PUT /api/v1/envelopes/{envelopeId}`
  - `POST /api/v1/envelopes/{envelopeId}/archive`
- Added new Ledger endpoint module:
  - `PlanningEndpoints.cs`
  - `PlanningEndpoints.Envelopes.cs`
- Wired planning endpoint group in Ledger bootstrap (`MapPlanningEndpoints`).
- Added Ledger integration test coverage for envelope list authorization boundaries.

## Validation
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -v minimal`
- `dotnet build DragonEnvelopes.sln -v minimal`
