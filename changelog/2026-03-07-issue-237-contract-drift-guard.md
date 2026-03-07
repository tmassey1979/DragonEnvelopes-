# Issue 237 - CI Contract Drift Guard

## Summary
Added a CI contract drift guard that compares API endpoint mappings with desktop client route usage and fails the build when mismatches are detected.

## Delivered
- Added `eng/verify-contract-drift.ps1`:
  - Scans server endpoint maps in:
    - `src/DragonEnvelopes.Api/Endpoints`
    - `src/DragonEnvelopes.Family.Api/Endpoints`
    - `src/DragonEnvelopes.Ledger.Api/Endpoints`
  - Scans desktop service calls in:
    - `client/DragonEnvelopes.Desktop/Services`
  - Normalizes route templates and compares method+path contracts.
  - Handles interpolated route segments with wildcard matching to reduce false positives on parameterized paths.
  - Writes report to `artifacts/contract-drift/contract-drift-report.md`.
  - Exits non-zero when mismatches are found to fail CI.
- Updated `.github/workflows/ci.yml`:
  - Added `Verify API and desktop contract drift` step in `build-test`.
  - Added always-on artifact upload for the drift report (`contract-drift-report`) so failures include diagnostics.

## Validation
- `./eng/verify-architecture.ps1 -RepoRoot .`
- `./eng/verify-contract-drift.ps1 -RepoRoot .`
- `dotnet build DragonEnvelopes.sln -v minimal`
