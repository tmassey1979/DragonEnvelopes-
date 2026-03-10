# Issue 299 - Event-Driven Orchestration: Onboarding and Approval Sagas

## Summary
Implemented persistent saga orchestration for onboarding and approval workflows, including timeline tracking, compensation/failure metadata, and family-scoped query endpoints in Family and Ledger APIs.

## Delivered
- Added saga domain entities:
  - `WorkflowSaga`
  - `WorkflowSagaTimelineEvent`
- Added persistence and repository support:
  - `IWorkflowSagaRepository`
  - `WorkflowSagaRepository`
  - EF configurations for saga and timeline tables
- Added saga orchestration service:
  - `ISagaOrchestrationService`
  - `SagaOrchestrationService`
  - shared constants for saga workflow types/statuses
- Added saga contracts:
  - `WorkflowSagaResponse`
  - `WorkflowSagaTimelineEventResponse`
- Added Family API saga endpoints:
  - `GET /api/v1/families/{familyId}/sagas`
  - `GET /api/v1/families/{familyId}/sagas/{sagaId}`
  - `GET /api/v1/families/{familyId}/sagas/{sagaId}/timeline`
- Added Ledger API saga endpoints:
  - `GET /api/v1/families/{familyId}/sagas`
  - `GET /api/v1/families/{familyId}/sagas/{sagaId}`
  - `GET /api/v1/families/{familyId}/sagas/{sagaId}/timeline`
- Integrated onboarding workflow with saga state/timeline steps:
  - onboarding request accepted
  - identity user creation/role assignment
  - family creation + primary guardian add
  - planning bootstrap requested/completed/failed
  - reconciliation/completion updates
  - compensation/failure recording on exceptions
- Integrated approval workflow with saga state/timeline steps:
  - request created/blocked
  - resolution started
  - resolution failed with compensation action marker
  - resolved-and-posted completion
  - deny path marked as compensated
- Added migration:
  - `20260310153001_AddWorkflowSagas`
- Added operational docs:
  - `docs/operations/workflow-sagas.md`

## Validation
- `dotnet test DragonEnvelopes.sln -c Release`
  - `DragonEnvelopes.Domain.Tests`: 6 passed
  - `DragonEnvelopes.Application.Tests`: 140 passed
  - `DragonEnvelopes.Desktop.Tests`: 102 passed
  - `DragonEnvelopes.Financial.Api.IntegrationTests`: 2 passed
  - `DragonEnvelopes.Family.Api.IntegrationTests`: 13 passed
  - `DragonEnvelopes.Ledger.Api.IntegrationTests`: 28 passed
  - `DragonEnvelopes.Api.IntegrationTests`: 113 passed

## Time Spent
- 3h 10m
