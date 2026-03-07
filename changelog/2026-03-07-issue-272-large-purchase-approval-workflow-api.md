# 2026-03-07 - Issue #272 - Large-Purchase Approval Workflow API

## Summary
- Added approval policy domain + persistence:
  - `FamilyApprovalPolicy`
  - per-family config with `IsEnabled`, `AmountThreshold`, and `RolesRequiringApproval`.
- Added approval request workflow domain + persistence:
  - `PurchaseApprovalRequest` with statuses (`Pending`, `Blocked`, `Approved`, `Denied`)
  - `PurchaseApprovalTimelineEvent` with audit event types (`Created`, `Blocked`, `Approved`, `Denied`)
- Added contracts under `DragonEnvelopes.Contracts.Approvals`:
  - policy upsert/get contracts
  - request create/list/resolve contracts
  - timeline response contract
- Added application service and repositories:
  - `IApprovalWorkflowService` / `ApprovalWorkflowService`
  - `IApprovalPolicyRepository` / `ApprovalPolicyRepository`
  - `IApprovalRequestRepository` / `ApprovalRequestRepository`
- Added ledger API approval endpoints:
  - `PUT /api/v1/approvals/policy`
  - `GET /api/v1/approvals/policy?familyId=...`
  - `POST /api/v1/approvals/requests`
  - `GET /api/v1/approvals/requests?familyId=...&status=...&take=...`
  - `GET /api/v1/approvals/requests/{requestId}/timeline?take=...`
  - `POST /api/v1/approvals/requests/{requestId}/approve`
  - `POST /api/v1/approvals/requests/{requestId}/deny`
- Integrated ledger posting path with approval blocking:
  - `POST /api/v1/transactions` now evaluates approval policy and returns `202 Accepted` with blocked approval request when applicable.
  - families without enabled policy retain normal transaction creation flow.
- Added FluentValidation for approval request/policy contracts.
- Added tests:
  - `ApprovalWorkflowServiceTests` (application)
  - ledger integration test for child block + parent approve + timeline + auth boundary.
  - updated test auth handler to support role header (`X-Test-Role`) and enforce parent-role policy in tests.
- Added EF migration:
  - `20260307170031_AddPurchaseApprovalWorkflow`

## Validation
- `dotnet build DragonEnvelopes.sln -c Release --nologo`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -c Release --nologo`
- `dotnet test tests/DragonEnvelopes.Ledger.Api.IntegrationTests/DragonEnvelopes.Ledger.Api.IntegrationTests.csproj -c Release --nologo`

## Notes
- Approval policy controls who is auto-blocked by role and amount threshold.
- Approve flow posts the actual ledger transaction and records timeline audit metadata.
