# 2026-03-05 - Desktop OIDC PKCE Sign-In

## Summary

Implemented desktop OIDC sign-in via system browser + loopback callback, persisted session tokens securely using DPAPI, and wired sign-in/sign-out status into the WPF shell.

## Completed Story

- #59 Implement desktop OIDC PKCE sign-in flow against Keycloak

## Key Changes

- Added OIDC client dependency for desktop:
  - `client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj`
- Added desktop auth layer:
  - `DesktopAuthService` for PKCE login and token capture
  - `LoopbackSystemBrowser` for system browser launch + callback capture (`HttpListener`)
  - `ProtectedTokenSessionStore` for encrypted token persistence via DPAPI
  - Auth contracts/models (`AuthSession`, `AuthSignInResult`, options/interfaces)
  - `client/DragonEnvelopes.Desktop/Auth/*.cs`
- Updated main shell view model:
  - Added restore-session startup path
  - Added async sign-in/sign-out command
  - Added auth status and action label bindings
  - `client/DragonEnvelopes.Desktop/ViewModels/MainWindowViewModel.cs`
- Updated shell UI:
  - Added auth status text and sign-in/sign-out button on topbar
  - `client/DragonEnvelopes.Desktop/MainWindow.xaml`
- Documented desktop OIDC settings and secure session storage:
  - `README.md`

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
