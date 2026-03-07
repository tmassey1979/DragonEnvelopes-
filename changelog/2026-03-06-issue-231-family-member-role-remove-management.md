# Issue #231: Family Member Role Update + Removal Management

## Summary
- Added parent-managed family member role updates and member removal across API and desktop workflows.
- Enforced safety guard preventing removal/demotion of the last remaining parent in a family.
- Added backend and desktop tests for new role/removal behavior and auth boundaries.

## Delivered
- Contracts:
  - Added `UpdateFamilyMemberRoleRequest` under `DragonEnvelopes.Contracts.Families`.
- Application:
  - Extended `IFamilyService` with `UpdateMemberRoleAsync(...)` and `RemoveMemberAsync(...)`.
  - Extended `IFamilyRepository` with member lookup/count/remove operations.
  - Implemented member role update and removal flows in `FamilyService`.
  - Added parent-presence guard so at least one parent remains assigned.
- Infrastructure:
  - Added repository implementations for tracked member fetch, role count, and member removal.
  - Updated invite pending check to provider-safe normalized comparison (removed provider-specific `ILike` usage in repository query).
- API (monolith + Family API):
  - Added `PUT /api/v1/families/{familyId}/members/{memberId}/role`.
  - Added `DELETE /api/v1/families/{familyId}/members/{memberId}`.
  - Added `UpdateFamilyMemberRoleRequestValidator` in both API projects.
- Desktop:
  - Extended `IFamilyMembersDataService`/`FamilyMembersDataService` with update-role and remove-member calls.
  - Enhanced `FamilyMembersViewModel` with selected-member state and commands:
    - `UpdateSelectedMemberRoleCommand`
    - `RemoveSelectedMemberCommand`
  - Updated family member UI template with selected-member role editor and remove action.
- Tests:
  - Added application tests for role update/remove + last-parent guard.
  - Added API integration tests for role update/remove auth boundaries and parent guard.
  - Added Family API integration smoke for member update/remove flow.
  - Added desktop view model tests for role update/remove commands.

## Validation
- `dotnet build DragonEnvelopes.sln -v minimal`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
- `dotnet test tests/DragonEnvelopes.Family.Api.IntegrationTests/DragonEnvelopes.Family.Api.IntegrationTests.csproj`
