# 2026-03-06 - Issue #146 - Parent Spend Notifications

## Summary
- Added webhook-driven parent spend notification generation.
- Added per-user channel preferences (Email/InApp/Sms) scoped by family membership.
- Added persisted notification event schema with retry-capable dispatch worker.

## Backend Changes
- Domain:
  - Added `NotificationPreference` entity.
  - Added `SpendNotificationEvent` entity (includes channel, envelope, amount, merchant, remaining balance, status, attempts).
- Infrastructure:
  - Added EF configurations and migration for:
    - `notification_preferences`
    - `spend_notification_events`
  - Added repositories:
    - `INotificationPreferenceRepository` / `NotificationPreferenceRepository`
    - `ISpendNotificationEventRepository` / `SpendNotificationEventRepository`
- Application:
  - Added notification DTOs.
  - Added services:
    - `IParentSpendNotificationService` / `ParentSpendNotificationService`
    - `ISpendNotificationDispatchService` / `SpendNotificationDispatchService`
  - Wired notification queue generation from `StripeWebhookService` on spend events.
  - Added dispatch retry behavior (`Queued` + attempt count, max attempts).
- API:
  - Added endpoints:
    - `GET /api/v1/families/{familyId}/notifications/preferences`
    - `PUT /api/v1/families/{familyId}/notifications/preferences`
  - Added hosted worker `SpendNotificationDispatchWorker` for background dispatch/retries.

## Acceptance Coverage
- Notification generated from eligible webhook spend events (`card_authorization`, `card_transaction` debit path).
- Notification payload includes envelope linkage, amount, merchant, and remaining balance.
- Channel preferences supported per family user (`Email`, `InApp`, `Sms`).
- Notification event schema includes retry/attempt metadata and failure states.

## Tests
- Added application tests:
  - queue generation respects parent preferences
  - dispatch service sends non-SMS channels and retries SMS when provider unavailable
- Updated webhook service tests for notification queue invocation.
- Added integration tests for notification preference family authorization boundaries.

## Validation Run
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --no-build`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --no-build`
