# 2026-03-06 - Issue #160 Data retention lifecycle for provider event tables

## Summary
Implemented scheduled data retention cleanup for provider event history tables to keep storage bounded while preserving recent operational diagnostics.

## Retention scope
- `stripe_webhook_events`: delete processed rows older than configured retention cutoff.
- `spend_notification_events`: delete terminal rows (`Sent`/`Failed`) older than configured retention cutoff.
- Active queued notifications are preserved.

## Implementation
- Added application retention service:
  - `IDataRetentionService`
  - `DataRetentionService`
  - `DataRetentionOptions` and `DataRetentionCleanupResult`
- Added repository cleanup methods:
  - `IStripeWebhookEventRepository.DeleteProcessedBeforeAsync(...)`
  - `ISpendNotificationEventRepository.DeleteTerminalBeforeAsync(...)`
- Added API hosted worker:
  - `DataRetentionWorker` (periodic cleanup loop)
- Registered worker in API startup (non-testing environments only).
- Added configurable settings in API appsettings:
  - `DataRetention:Enabled`
  - `DataRetention:PollIntervalMinutes`
  - `DataRetention:BatchSize`
  - `DataRetention:StripeWebhookRetentionDays`
  - `DataRetention:SpendNotificationRetentionDays`

## Tests
- Added `DataRetentionServiceTests` covering:
  - configured cutoff/batch behavior
  - normalization of invalid config values

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
