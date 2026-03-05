# 2026-03-05 - desktop onboarding flow and resize-safe auth forms

## Summary
- Changed app startup to show login first and only show main window after successful login.
- Added login cancel action that exits app when login is not completed.
- Made login and create-family windows resize-friendly with scrollable content to prevent clipped controls.
- Wired create-family flow to backend APIs (create family + add parent member).
- Allowed anonymous access to family bootstrap endpoints for first-time onboarding.

## Files Changed
- client/DragonEnvelopes.Desktop/App.xaml
- client/DragonEnvelopes.Desktop/App.xaml.cs
- client/DragonEnvelopes.Desktop/Controls/LoginControl.xaml
- client/DragonEnvelopes.Desktop/Controls/LoginControl.xaml.cs
- client/DragonEnvelopes.Desktop/MainWindow.xaml.cs
- client/DragonEnvelopes.Desktop/Views/LoginWindow.xaml
- client/DragonEnvelopes.Desktop/Views/LoginWindow.xaml.cs
- client/DragonEnvelopes.Desktop/Views/CreateFamilyAccountWindow.xaml
- client/DragonEnvelopes.Desktop/Views/CreateFamilyAccountWindow.xaml.cs
- client/DragonEnvelopes.Desktop/Services/FamilyAccountService.cs
- client/DragonEnvelopes.Desktop/Services/FamilyAccountServiceFactory.cs
- client/DragonEnvelopes.Desktop/Services/FamilyAccountCreateResult.cs
- src/DragonEnvelopes.Api/Program.cs
- README.md

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release`
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release --no-build`
