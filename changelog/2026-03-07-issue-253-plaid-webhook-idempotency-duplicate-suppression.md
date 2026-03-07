# Issue 253 - Plaid Webhook Idempotency and Duplicate Suppression

## Summary
Added deterministic duplicate suppression for Plaid webhooks so retried deliveries do not re-run transaction sync or balance refresh workflows.

## Delivered
- Updated `/api/v1/webhooks/plaid` duplicate handling:
  - Added deterministic duplicate detection based on webhook type/code/item id + payload equality.
  - Limited duplicate lookup to a bounded 72-hour window to avoid unbounded scans.
  - Added explicit duplicate outcome persistence (`ProcessingStatus = Duplicate`) for timeline visibility.
  - Duplicate deliveries now return success (`Outcome = Duplicate`) and skip downstream processing.
- Added integration tests:
  - `Plaid_Webhook_Transactions_Duplicate_Delivery_Is_Suppressed`
  - `Plaid_Webhook_Balance_Duplicate_Delivery_Is_Suppressed`
- Verified duplicate suppression through service call-count assertions and persisted status checks.

## Validation
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj --configuration Release --no-restore`
  - Passed: 104, Failed: 0
