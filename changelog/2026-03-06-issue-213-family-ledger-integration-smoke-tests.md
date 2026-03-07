# Issue #213 - Family/Ledger Integration Smoke Tests

## Delivered
- Added new integration test project: `tests/DragonEnvelopes.Family.Api.IntegrationTests`.
- Added new integration test project: `tests/DragonEnvelopes.Ledger.Api.IntegrationTests`.
- Implemented smoke tests in each project to verify:
  - host boots successfully
  - `GET /health/live` returns `200 OK`
  - unauthenticated request to protected endpoint returns `401 Unauthorized`
- Added test factories using:
  - in-memory `DragonEnvelopesDbContext` override
  - test authentication handler (no external IdP dependency)
- Added both projects to `DragonEnvelopes.sln`.
- Updated `README.md` solution list to include new test projects.

## Validation
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
