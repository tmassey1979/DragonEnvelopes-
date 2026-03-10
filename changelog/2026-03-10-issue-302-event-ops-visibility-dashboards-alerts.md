# Issue 302 - Event Operations Visibility Dashboards and Alerts

## Summary
Implemented event pipeline operational telemetry with structured metric emission, Grafana dashboarding for lag/retry/failure/DLQ health, alert rule definitions, and an operations runbook with triage + escalation guidance.

## Delivered
- Added event pipeline metric emission to outbox dispatch service:
  - `event.publish.lag.seconds`
  - `event.publish.published.count`
  - `event.publish.failure.count`
  - `event.publish.backlog.count`
  - Includes source service, routing key, stage, queue, and correlation id dimensions.
- Added event pipeline metric emission to Financial ledger consumer:
  - `event.consumer.lag.seconds`
  - `event.consumer.retry.count`
  - `event.consumer.failure.count`
  - `event.consumer.deadletter.count`
  - `event.queue.main.depth`
  - `event.queue.retry.depth`
  - `event.queue.dlq.depth`
  - Added correlation-aware logging context for retry/dead-letter paths.
- Added Grafana dashboard:
  - `infrastructure/observability/grafana/dashboards/dragonenvelopes-event-pipeline-health.json`
  - Panels for publish lag, consumer lag, retries, failures, DLQ depth, and correlation-id drill-down metric stream.
- Added alert rule definition set:
  - `infrastructure/observability/grafana/alert-rules/event-pipeline-alert-rules.yml`
  - Includes sustained lag, retry spike, failure spike, and DLQ growth thresholds.
- Added operations runbook:
  - `docs/operations/event-pipeline-observability.md`
  - Includes metric catalog, triage checklist, mitigation guidance, escalation paths, and post-mitigation validation.
- Updated docs index and references:
  - `docs/README.md`
  - `docs/technical-guide.md`
  - `docs/operations/rabbitmq-consumer-reliability.md`
- Added tests for metric emission behavior in outbox dispatch service:
  - `tests/DragonEnvelopes.Application.Tests/IntegrationOutboxDispatchServiceTests.cs`

## Validation
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -c Release --filter IntegrationOutboxDispatchServiceTests`
  - 3 passed
- `dotnet test DragonEnvelopes.sln -c Release`
  - `DragonEnvelopes.Domain.Tests`: 6 passed
  - `DragonEnvelopes.Application.Tests`: 143 passed
  - `DragonEnvelopes.Desktop.Tests`: 102 passed
  - `DragonEnvelopes.Financial.Api.IntegrationTests`: 6 passed
  - `DragonEnvelopes.Family.Api.IntegrationTests`: 13 passed
  - `DragonEnvelopes.Ledger.Api.IntegrationTests`: 31 passed
  - `DragonEnvelopes.Api.IntegrationTests`: 113 passed
- `./eng/verify-contract-drift.ps1 -RepoRoot .`
- `./eng/verify-event-contract-compatibility.ps1 -RepoRoot .`
- Dashboard JSON validation:
  - `Get-Content infrastructure/observability/grafana/dashboards/dragonenvelopes-event-pipeline-health.json | ConvertFrom-Json | Out-Null`

## Time Spent
- 1h 45m
