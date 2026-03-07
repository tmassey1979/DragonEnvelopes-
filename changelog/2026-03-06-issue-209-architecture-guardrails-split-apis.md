# Issue #209 - Architecture Guardrails for Split APIs

## Delivered
- Extended `eng/verify-architecture.ps1` to validate dependency boundaries for:
  - `DragonEnvelopes.Family.Api`
  - `DragonEnvelopes.Ledger.Api`
- Added explicit verification for:
  - `DragonEnvelopes.Contracts` having no project references
  - `DragonEnvelopes.ProviderClients` referencing `Application` and `Domain`
- Standardized dependency assertions with helper functions for clearer failures.
- Updated `README.md` solution and architecture dependency sections to reflect current split-service topology.

## Validation
- `./eng/verify-architecture.ps1 -RepoRoot .`
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
