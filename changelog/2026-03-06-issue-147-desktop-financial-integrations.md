# 2026-03-06 - Issue #147 - Desktop Plaid/Stripe Integrations Workspace

## Summary
Implemented a new WPF `Financial Integrations` workspace that connects Phase 2 backend APIs to desktop UI for:
- family financial status and spend notification preference updates
- Plaid onboarding and account-link management
- Plaid transaction sync, balance refresh, and reconciliation visibility
- Stripe setup intent creation
- envelope financial account linking
- virtual/physical card issuing and card lifecycle actions
- spending controls, spend evaluation, and card control audit visibility

## Files
- client/DragonEnvelopes.Desktop/Navigation/RouteRegistry.cs
- client/DragonEnvelopes.Desktop/Resources/Templates/ShellTemplates.xaml
- client/DragonEnvelopes.Desktop/Services/IFinancialIntegrationDataService.cs
- client/DragonEnvelopes.Desktop/Services/FinancialIntegrationDataService.cs
- client/DragonEnvelopes.Desktop/ViewModels/MainWindowViewModel.cs
- client/DragonEnvelopes.Desktop/ViewModels/FinancialIntegrationsViewModel.cs
- client/DragonEnvelopes.Desktop/ViewModels/PlaidAccountLinkItemViewModel.cs
- client/DragonEnvelopes.Desktop/ViewModels/PlaidReconciliationAccountItemViewModel.cs
- client/DragonEnvelopes.Desktop/ViewModels/EnvelopeFinancialAccountItemViewModel.cs
- client/DragonEnvelopes.Desktop/ViewModels/PaymentCardItemViewModel.cs
- client/DragonEnvelopes.Desktop/ViewModels/CardControlAuditItemViewModel.cs

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj` (pass)
- `dotnet build DragonEnvelopes.sln` (pass)
- `dotnet test tests/DragonEnvelopes.Domain.Tests/DragonEnvelopes.Domain.Tests.csproj --no-build` (pass)
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --no-build` (pass)
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --no-build` (pass)

## Notes
- Left pre-existing local changes untouched:
  - `client/DragonEnvelopes.Desktop/Assets/dragonenvelopes-logo-transparent.png`
  - untracked `Codex/*.md` working docs
