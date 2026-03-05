# 2026-03-05 - create family account window

## Summary
- Added a dedicated `CreateFamilyAccountWindow` for household bootstrap input.
- Added client-side validation for required fields, email format, and password confirmation.
- Added family account service contract scaffolding and placeholder implementation.
- Wired login flow `Create Family Account` action to open the new window.

## Files Changed
- client/DragonEnvelopes.Desktop/Views/CreateFamilyAccountWindow.xaml
- client/DragonEnvelopes.Desktop/Views/CreateFamilyAccountWindow.xaml.cs
- client/DragonEnvelopes.Desktop/Views/LoginWindow.xaml.cs
- client/DragonEnvelopes.Desktop/Services/IFamilyAccountService.cs
- client/DragonEnvelopes.Desktop/Services/FamilyAccountService.cs
- client/DragonEnvelopes.Desktop/Services/CreateFamilyAccountRequest.cs
- client/DragonEnvelopes.Desktop/Services/FamilyAccountCreateResult.cs

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release`
