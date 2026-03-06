# 2026-03-06 - Issues #155 and #156

## Summary
- Added end-to-end trace correlation support for financial integration operations.
- Surfaced/copy-enabled trace id in desktop Financial Integrations panel.

## Issue #155 - Observability trace identifiers
- Added `TraceId` to `ProviderActivityHealthResponse`.
- Added provider activity payload trace id mapping from `HttpContext.TraceIdentifier`.
- Added `X-Trace-Id` response header for all financial integration endpoints via endpoint filter.
- Extended integration tests to verify:
  - provider activity payload includes non-empty `TraceId`
  - financial status and provider activity include `X-Trace-Id` header

## Issue #156 - Desktop trace id UX
- Added `ProviderHealthTraceId` state in `FinancialIntegrationsViewModel`.
- Added `CopyProviderTraceIdCommand` with clipboard handling and local security event logging.
- Updated provider health panel UI with trace id display and `Copy Trace Id` action.
- Extended desktop smoke tests to verify trace id loads from provider activity data.

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --no-build`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --no-build`
