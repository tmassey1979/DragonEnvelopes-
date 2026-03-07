# RabbitMQ Consumer Reliability Runbook

This runbook covers the Financial service ledger transaction consumer reliability baseline (`#294`).

## Queue Topology
- Main queue: `dragonenvelopes.financial.ledger-transaction-created`
- Retry queue: `dragonenvelopes.financial.ledger-transaction-created.retry`
- Dead-letter queue: `dragonenvelopes.financial.ledger-transaction-created.dlq`
- Main routing key: `ledger.transaction.created.v1`
- Retry routing key: `ledger.transaction.created.retry.v1`
- DLQ routing key: `ledger.transaction.created.dlq.v1`

## Idempotency Strategy
- Consumer name: `financial.ledger-transaction-created-consumer`
- Deterministic inbox key: `consumerName:sourceService:eventId` (normalized lowercase)
- Inbox table: `integration_inbox_messages`
- Duplicate behavior:
  - Already processed event key -> `ack` without re-processing.
  - Already dead-lettered event key -> `ack` without re-processing.

## Retry and Poison Handling
- Retry attempts: configured by `Messaging:RabbitMq:ConsumerMaxRetryAttempts` (default `5`).
- Retry delay: configured by `Messaging:RabbitMq:ConsumerRetryDelayMilliseconds` (default `30000`).
- Failures before max attempts:
  - Message is republished to retry routing key.
  - Retry queue TTL sends message back to main queue.
- Failures at/after max attempts:
  - Message is published to DLQ routing key.
  - Inbox entry is marked dead-lettered.
- Poison payload (invalid envelope/payload):
  - Message is directly dead-lettered.
  - Inbox entry is stored with hash-derived idempotency key.

## Observability
- Consumer logs queue depth snapshots on startup and on dead-letter events.
- Consumer logs include:
  - idempotency key
  - attempt count
  - dead-lettered inbox count
- DB inspection:
  - `integration_inbox_messages` shows attempt history, processed/dead-letter timestamps, and last error.

## Replay Procedure (DLQ -> Main)
1. Identify candidate messages in `dragonenvelopes.financial.ledger-transaction-created.dlq`.
2. Verify root cause is fixed (schema mismatch, transient dependency failure, etc.).
3. Replay each message to routing key `ledger.transaction.created.v1`.
4. Monitor consumer logs and `integration_inbox_messages` for successful processing.
5. Keep replay batches small to avoid burst retries.

## Replay Notes
- Replayed messages keep the same `eventId`; if already processed, inbox idempotency safely suppresses duplicates.
- If a message is still poison, it will return to DLQ with updated attempt/error metadata.
