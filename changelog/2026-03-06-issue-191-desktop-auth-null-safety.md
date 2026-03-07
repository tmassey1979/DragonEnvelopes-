# Issue #191 - Desktop Auth Null-Safety Hardening

## Delivered
- Hardened `DesktopAuthService` against malformed/partial session state during startup and token retrieval.
- Added defensive restore behavior:
  - invalid or missing access token in persisted session now falls back to signed-out state.
  - restore flow failures now safely sign out instead of surfacing runtime exceptions.
- Added additional guarded fallback in `GetAccessTokenAsync` so session/refresh failures return unauthenticated state rather than throwing.
- Added resilient refresh-result handling:
  - refresh payload is validated before building a refreshed session.
  - malformed refresh results now gracefully fall back to existing valid session or sign-out if expired.
- Added OIDC client abstraction for testability:
  - `IDesktopOidcClient`
  - `DesktopOidcClientAdapter`
- Added desktop auth unit tests for null/partial/malformed paths:
  - invalid stored session payload
  - refresh exception with non-expired session fallback
  - null refresh result with expired session fallback
  - session store load exception fallback

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --filter "DesktopAuthServiceTests" -v minimal`
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
