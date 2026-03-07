# Issue #197 - Family Endpoint Modularization

## Delivered
- Refactored `FamilyEndpoints` into concern-based partial endpoint mapping files:
  - `FamilyEndpoints.Core.cs`
  - `FamilyEndpoints.MembersAndInvites.cs`
  - `FamilyEndpoints.Onboarding.cs`
- Simplified `FamilyEndpoints.cs` to orchestration-only registration.
- Preserved external API behavior:
  - same route paths
  - same auth policies
  - same operation names/OpenAPI metadata
  - same entry point (`MapFamilyEndpoints`)
- Fixed onboarding split boundary to include full bootstrap endpoint chain after initial extraction.

## Validation
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
