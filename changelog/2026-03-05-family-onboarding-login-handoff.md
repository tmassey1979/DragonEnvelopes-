# 2026-03-05 - family onboarding login handoff polish

## Summary
- Added login status messaging support with success/error rendering.
- Added username/email prefill support in login control.
- After successful Create Family flow, login now shows success message and pre-populates email.

## Files Changed
- client/DragonEnvelopes.Desktop/Controls/LoginControl.xaml
- client/DragonEnvelopes.Desktop/Controls/LoginControl.xaml.cs
- client/DragonEnvelopes.Desktop/Views/CreateFamilyAccountWindow.xaml.cs
- client/DragonEnvelopes.Desktop/Views/LoginWindow.xaml.cs

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release`
