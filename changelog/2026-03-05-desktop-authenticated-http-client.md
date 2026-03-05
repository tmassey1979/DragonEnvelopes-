# 2026-03-05 - Desktop Authenticated HTTP Client Pipeline

## Summary

Implemented a centralized authenticated HTTP client pipeline in the desktop app that attaches bearer tokens, refreshes when near expiry, and safely retries idempotent requests on `401`.

## Completed Story

- #60 Implement authenticated HTTP client with token attach and refresh

## Key Changes

- Added authenticated API client layer:
  - `ApiClientOptions`
  - `IBackendApiClient`
  - `DragonEnvelopesApiClient`
  - `AuthenticatedApiHttpMessageHandler`
  - `client/DragonEnvelopes.Desktop/Api/*.cs`
- Extended auth service contract and implementation:
  - Added `GetAccessTokenAsync(forceRefresh)` to auth service interface
  - Added in-memory session caching and refresh-before-expiry behavior
  - Added forced refresh path for retry scenarios
  - `client/DragonEnvelopes.Desktop/Auth/IAuthService.cs`
  - `client/DragonEnvelopes.Desktop/Auth/DesktopAuthService.cs`
- Integrated API pipeline in shell view model:
  - Builds authenticated `HttpClient` with delegated token handler
  - Added `PingApiCommand` and API status field for quick pipeline verification
  - `client/DragonEnvelopes.Desktop/ViewModels/MainWindowViewModel.cs`
  - `client/DragonEnvelopes.Desktop/MainWindow.xaml`
- Updated docs:
  - Desktop authenticated client behavior and API base URL override
  - `README.md`

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
