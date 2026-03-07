# Issue #186 - Role Matrix Hardening for Parent-Only Operations

## Delivered
- Added parent-only policy enforcement for provider secret rewrap endpoint:
  - `POST /api/v1/families/{familyId}/financial/security/rewrap-provider-secrets`
- Added desktop UI gating for parent-only manual recurring auto-post control:
  - `Run Auto-Post Now` button is disabled when `IsParentUser` is false.
- Extended integration test auth harness to support role injection via header:
  - `X-Test-Role`
- Added integration role-matrix tests:
  - teen with family access cannot run manual recurring auto-post
  - adult with family access cannot rewrap provider secrets

## Validation
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass

## Notes
- Existing family-access guard remains in place; role policy now additionally enforces parent-only operations.
