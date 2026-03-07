# Issue 256: Configurable Month-End Envelope Rollover

## Summary
- Added per-envelope rollover policy support (`None`, `Full`, `Cap`) with optional cap values.
- Added deterministic month-end rollover preview and idempotent apply workflow with persisted audit runs per family/month.
- Added desktop budget workspace controls to edit rollover policy, preview month-end carry-forward totals, and apply rollover.

## Backend Changes
- Domain:
  - Added `EnvelopeRolloverMode` and `EnvelopeRolloverCalculator`.
  - Extended `Envelope` with `RolloverMode`, `RolloverCap`, policy update, and month-end apply behavior.
  - Added `EnvelopeRolloverRun` audit entity.
- Application:
  - Added `IEnvelopeRolloverService` + `EnvelopeRolloverService`.
  - Added rollover DTOs for preview/apply itemization.
  - Extended `EnvelopeService` and `IEnvelopeService` for rollover policy updates.
  - Added `IEnvelopeRolloverRunRepository`.
- Infrastructure:
  - Added `EnvelopeRolloverRunRepository`.
  - Extended `EnvelopeRepository` with `ListByFamilyForUpdateAsync`.
  - Added EF configuration for rollover run table and envelope rollover fields.
  - Added migration `20260307102505_AddEnvelopeRolloverPolicies`.
- API / Ledger routes:
  - `PUT /api/v1/envelopes/{envelopeId}/rollover-policy`
  - `GET /api/v1/budgets/rollover/preview?familyId={id}&month={yyyy-MM}`
  - `POST /api/v1/budgets/rollover/apply`
  - Added request validators for rollover policy and apply request contracts.

## Desktop Changes
- Budget workspace now hydrates envelope rollover policy data.
- Added per-envelope rollover mode/cap editing and save action.
- Added rollover preview/apply actions with summary status messages.

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
