# Workflow Sagas Operations Guide

## Purpose
Critical long-running workflows now persist saga state and timeline events so support can inspect progress, failure reasons, and compensation outcomes.

Initial workflows covered:
- `Onboarding`
- `Approval`

## Data Model
- Saga table: `workflow_sagas`
- Timeline table: `workflow_saga_timeline_events`

`workflow_sagas` tracks current state:
- `WorkflowType`
- `CorrelationId`
- `ReferenceId`
- `Status`
- `CurrentStep`
- `FailureReason`
- `CompensationAction`
- timestamps (`StartedAtUtc`, `UpdatedAtUtc`, `CompletedAtUtc`)

`workflow_saga_timeline_events` tracks append-only history:
- `Step`
- `EventType`
- `Status`
- `Message`
- `OccurredAtUtc`

## API Endpoints

Family API:
- `GET /api/v1/families/{familyId}/sagas?workflowType={workflowType?}&take={take?}`
- `GET /api/v1/families/{familyId}/sagas/{sagaId}`
- `GET /api/v1/families/{familyId}/sagas/{sagaId}/timeline?take={take?}`

Ledger API:
- `GET /api/v1/families/{familyId}/sagas?workflowType={workflowType?}&take={take?}`
- `GET /api/v1/families/{familyId}/sagas/{sagaId}`
- `GET /api/v1/families/{familyId}/sagas/{sagaId}/timeline?take={take?}`

All endpoints enforce family authorization boundaries.

## Expected Status Semantics
- `Running`: workflow is active.
- `Completed`: workflow reached terminal success.
- `Failed`: workflow hit an error and needs review/retry.
- `Compensated`: workflow ended after explicit compensation.

## Typical Investigation Steps
1. List sagas for family + workflow.
2. Open the latest saga and review `Status`, `CurrentStep`, and `FailureReason`.
3. Query timeline and inspect ordered events for failure/compensation transitions.
4. Correlate `CorrelationId` and `ReferenceId` with related workflow records.

## Current Compensation Paths
- Onboarding: best-effort Keycloak user deletion when family creation/member linking fails.
- Approval: request remains pending for manual retry when transaction posting fails; deny path is recorded as compensated with no transaction posted.
