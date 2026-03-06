# 2026-03-06 - Issue #153

## Summary
- Hardened desktop token refresh flow to prevent null-reference crashes in `DesktopAuthService.EnsureSessionAsync`.

## Delivered
- Added defensive session validation:
  - sign out safely when stored session is null/invalid.
- Added resilient refresh behavior:
  - if refresh throws or returns invalid payload and current token is still unexpired, keep current session.
  - if refresh fails and token is expired, sign out safely.
- Wrapped `EnsureSessionAsync` with top-level defensive catch to prevent exception leakage.
- Added `SafeSignOutAsync` fallback to avoid secondary sign-out exceptions from crashing token retrieval paths.

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --no-build`
- `dotnet build DragonEnvelopes.sln`
