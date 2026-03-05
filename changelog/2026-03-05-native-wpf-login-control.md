# 2026-03-05 - native wpf login control

## Summary
- Added a reusable native `LoginControl` for username/password sign-in.
- Added an in-app `LoginWindow` and wired top-bar auth button to open it.
- Added `SignInWithPasswordAsync` path in desktop auth service to avoid external browser redirect.
- Updated main shell auth workflow to support sign-out and native sign-in status handling.

## Files Changed
- client/DragonEnvelopes.Desktop/Auth/IAuthService.cs
- client/DragonEnvelopes.Desktop/Auth/DesktopAuthService.cs
- client/DragonEnvelopes.Desktop/MainWindow.xaml
- client/DragonEnvelopes.Desktop/MainWindow.xaml.cs
- client/DragonEnvelopes.Desktop/ViewModels/MainWindowViewModel.cs
- client/DragonEnvelopes.Desktop/Controls/LoginControl.xaml
- client/DragonEnvelopes.Desktop/Controls/LoginControl.xaml.cs
- client/DragonEnvelopes.Desktop/Views/LoginWindow.xaml
- client/DragonEnvelopes.Desktop/Views/LoginWindow.xaml.cs

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release`
