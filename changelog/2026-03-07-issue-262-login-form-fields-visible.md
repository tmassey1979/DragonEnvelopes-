# 2026-03-07 Issue #262 - Login form required fields visible at launch

## Summary
- Fixed desktop login entry behavior so the sign-in form is shown by default when the login window opens.
- Added a direct `Create Family` action to the sign-in action row so onboarding remains available without signing in first.
- Preserved existing back/cancel/redeem-invite/create-from-invite/sign-in behaviors.

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -c Release --nologo` (passed)
- `dotnet build DragonEnvelopes.sln -c Release --nologo` (passed)

## Notes
- Debug configuration test run was blocked by local file locks (`devenv.exe` / running desktop app). Release validation was used to avoid touching locked Debug outputs.
