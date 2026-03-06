# 2026-03-06 - Issue #149 - Native Plaid Link Flow in Desktop

## Summary
Implemented a native Plaid Link browser callback workflow in WPF so family admins can connect Plaid without manual public token copy/paste.

## Delivered
- Added desktop Plaid Link service stack:
  - `IDesktopPlaidLinkService`
  - `DesktopPlaidLinkService`
  - `DesktopPlaidLinkOptions`
  - `DesktopPlaidLinkResult`
- Service behavior:
  - Starts secure loopback `HttpListener` callback endpoint
  - Opens system browser to local launch page
  - Loads Plaid Link JS and handles success/exit outcomes
  - Validates callback `state`
  - Returns public token or cancellation/failure result
- WPF integrations workspace updates:
  - Added `LaunchNativePlaidLinkCommand`
  - Auto-creates link token if missing
  - Automatically exchanges returned public token via API
  - Clears token fields after exchange
  - Provides cancellation and error status handling
- Navigation wiring:
  - Route registry now injects `DesktopPlaidLinkService` into `FinancialIntegrationsViewModel`
- UI updates:
  - Added "Launch Native Plaid Link" action in Plaid section

## Files
- client/DragonEnvelopes.Desktop/Services/IDesktopPlaidLinkService.cs
- client/DragonEnvelopes.Desktop/Services/DesktopPlaidLinkService.cs
- client/DragonEnvelopes.Desktop/Services/DesktopPlaidLinkOptions.cs
- client/DragonEnvelopes.Desktop/Services/DesktopPlaidLinkResult.cs
- client/DragonEnvelopes.Desktop/ViewModels/FinancialIntegrationsViewModel.cs
- client/DragonEnvelopes.Desktop/Navigation/RouteRegistry.cs
- client/DragonEnvelopes.Desktop/Resources/Templates/ShellTemplates.xaml

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj` (pass)
- `dotnet build DragonEnvelopes.sln` (pass)
- `dotnet test tests/DragonEnvelopes.Domain.Tests/DragonEnvelopes.Domain.Tests.csproj --no-build` (pass)
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --no-build` (pass)
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --no-build` (pass)

## Notes
- Existing unstaged local assets/docs were intentionally left untouched.
