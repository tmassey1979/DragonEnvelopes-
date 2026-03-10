# Issue 300 - Event Contract Compatibility Tests and CI Gates

## Summary
Implemented event contract compatibility governance with machine-readable catalog validation tests, consumer compatibility coverage, and CI enforcement/reporting for breaking changes.

## Delivered
- Added machine-readable event contract catalog:
  - `docs/architecture/event-contract-catalog-v1.json`
- Added catalog generator utility:
  - `eng/generate-event-contract-catalog.ps1`
- Added breaking-change policy file:
  - `eng/event-contract-breaking-change-policy.json`
- Added CI compatibility verifier:
  - `eng/verify-event-contract-compatibility.ps1`
  - compares current contract catalog to baseline (`origin/<base>` or `HEAD~1`)
  - detects removed contracts, event/source/payload-type drift, and required-field removals
  - fails on unapproved breaking changes
  - publishes markdown report
- Added producer contract tests:
  - `EventContractCatalogTests` validates routing keys, canonical event names, payload contract field coverage, and envelope required metadata validation alignment.
- Added consumer compatibility tests:
  - minor schema version (`1.1`) accepted
  - unsupported major schema (`2.0`) dead-lettered
  - legacy raw payload compatibility preserved
  - invalid envelope metadata dead-letter behavior asserted
- Updated Financial consumer compatibility handling:
  - distinguishes envelope vs legacy raw payload shape and applies strict validation/fallback accordingly.
- Updated CI workflow:
  - fetches full git history in build job (`fetch-depth: 0`) for baseline comparisons
  - runs `verify-event-contract-compatibility.ps1`
  - uploads `event-contract-compatibility-report` artifact
- Updated docs index and event catalog narrative to reference machine-readable catalog.

## Validation
- `dotnet test DragonEnvelopes.sln -c Release`
  - `DragonEnvelopes.Domain.Tests`: 6 passed
  - `DragonEnvelopes.Application.Tests`: 143 passed
  - `DragonEnvelopes.Desktop.Tests`: 102 passed
  - `DragonEnvelopes.Financial.Api.IntegrationTests`: 6 passed
  - `DragonEnvelopes.Family.Api.IntegrationTests`: 13 passed
  - `DragonEnvelopes.Ledger.Api.IntegrationTests`: 28 passed
  - `DragonEnvelopes.Api.IntegrationTests`: 113 passed
- `./eng/verify-contract-drift.ps1 -RepoRoot .`
- `./eng/verify-event-contract-compatibility.ps1 -RepoRoot .`

## Time Spent
- 2h 35m
