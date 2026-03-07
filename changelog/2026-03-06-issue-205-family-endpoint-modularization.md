# Issue #205 - Family API Endpoint Modularization

## Delivered
- Split `src/DragonEnvelopes.Family.Api/Endpoints/FamilyEndpoints.cs` into concern-focused partial endpoint files:
  - `src/DragonEnvelopes.Family.Api/Endpoints/FamilyEndpoints.Core.cs`
  - `src/DragonEnvelopes.Family.Api/Endpoints/FamilyEndpoints.MembersAndInvites.cs`
  - `src/DragonEnvelopes.Family.Api/Endpoints/FamilyEndpoints.Onboarding.cs`
- Reduced `FamilyEndpoints.cs` to an aggregator that composes the concern mappers.
- Preserved existing route mappings, route names, auth policies, and response behavior.

## Validation
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
