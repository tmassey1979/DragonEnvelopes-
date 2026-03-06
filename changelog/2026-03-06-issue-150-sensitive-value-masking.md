# 2026-03-06 - Issue #150 - Sensitive Value Masking in Desktop Integrations UI

## Summary
Implemented masking and explicit reveal/copy controls for sensitive Plaid/Stripe values in the desktop Financial Integrations workspace, with local security event logging for user-initiated reveal/copy actions.

## Delivered
- Added masking helper:
  - `SensitiveValueMasker`
- Added security event model:
  - `SecurityAuditEventItemViewModel`
- Updated `FinancialIntegrationsViewModel`:
  - masked display properties for sensitive values and identifiers
  - reveal toggles for link token, client secret, and provider identifiers
  - explicit copy commands for link token and client secret
  - local security event log collection with capped history
  - secure summary displays use masked identifiers
- Updated mappings to mask identifiers by default in workspace tables:
  - Plaid account links
  - Envelope financial accounts
  - Card provider identifiers
- Updated XAML template:
  - reveal/copy controls for Plaid link token and Stripe client secret
  - identifier reveal toggle in status panel
  - security event log card in integrations workspace

## Files
- client/DragonEnvelopes.Desktop/ViewModels/SensitiveValueMasker.cs
- client/DragonEnvelopes.Desktop/ViewModels/SecurityAuditEventItemViewModel.cs
- client/DragonEnvelopes.Desktop/ViewModels/FinancialIntegrationsViewModel.cs
- client/DragonEnvelopes.Desktop/Resources/Templates/ShellTemplates.xaml

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj` (pass)
- `dotnet build DragonEnvelopes.sln` (pass)
- `dotnet test tests/DragonEnvelopes.Domain.Tests/DragonEnvelopes.Domain.Tests.csproj --no-build` (pass)
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --no-build` (pass)
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --no-build` (pass)

## Notes
- Existing unstaged local asset/docs changes were intentionally left untouched.
