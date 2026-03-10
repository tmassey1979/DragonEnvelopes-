# Event Pipeline Observability Runbook

This runbook covers eventing visibility for outbox publishing and Financial consumer processing (`#302`).

## Dashboard and Rules
- Grafana dashboard: `infrastructure/observability/grafana/dashboards/dragonenvelopes-event-pipeline-health.json`
  - UID: `dragonenvelopes-event-pipeline`
- Alert rule set (query + threshold definitions):
  - `infrastructure/observability/grafana/alert-rules/event-pipeline-alert-rules.yml`

## Emitted Metrics (Structured Telemetry)
The platform emits `EventPipelineMetric` logs with these key metrics:

- `event.publish.lag.seconds`: outbox event lag from `OccurredAtUtc` to publish attempt.
- `event.publish.published.count`: published events per outbox cycle.
- `event.publish.failure.count`: publish failures per event and per cycle.
- `event.publish.backlog.count`: pending outbox backlog count.
- `event.consumer.lag.seconds`: consumer lag from message timestamp to consumption time.
- `event.consumer.retry.count`: retry events routed to retry queue.
- `event.consumer.failure.count`: consumer failures (retry/dead-letter/exception paths).
- `event.consumer.deadletter.count`: dead-lettered events.
- `event.queue.main.depth`: main queue depth snapshot.
- `event.queue.retry.depth`: retry queue depth snapshot.
- `event.queue.dlq.depth`: dead-letter queue depth snapshot.

Each metric record includes `CorrelationId`, `RoutingKey`, `Queue`, `SourceService`, and `Stage` for drill-down.

## Triage Checklist
1. Open Grafana dashboard `DragonEnvelopes Event Pipeline Health` and check publish lag and consumer lag trends.
2. If lag is rising, inspect outbox backlog (`event.publish.backlog.count`) to identify source service pressure.
3. Check retry/failure panels to determine whether failures are transient (retry spike) or systemic (failure spike).
4. If DLQ depth is non-zero, inspect DLQ growth trend and correlate with retry/failure spikes.
5. Use the dashboard correlation filter and log panel to trace a single `CorrelationId` across publish + consume stages.
6. Confirm RabbitMQ queue depths (`main/retry/dlq`) and inbox/outbox DB state for the implicated event family.
7. Apply remediation:
   - transient dependency failures: stabilize dependency and monitor retry drain,
   - payload/schema issues: deploy compatible consumer fix, then replay from DLQ,
   - source publish pressure: scale worker throughput (`batchSize`, worker poll interval) and monitor lag recovery.

## Escalation Paths
- `warning` alerts (lag/retry sustained):
  - On-call application engineer owns first response.
  - Escalate to platform engineer if warning persists beyond 30 minutes.
- `critical` alerts (failure spike or DLQ growth):
  - Immediate escalation to platform on-call + service owner.
  - If customer impact is detected, engage incident commander and post incident status updates.

## Validation Steps After Mitigation
1. Confirm retry and failure rates return to baseline.
2. Confirm DLQ depth trends to zero (or remains stable at zero for at least 15 minutes).
3. Confirm publish and consumer lag are below warning thresholds.
4. Validate sampled business flows in app/UI that depend on ledger event processing.
