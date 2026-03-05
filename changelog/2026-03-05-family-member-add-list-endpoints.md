# 2026-03-05 - family member add list endpoints

## Summary
- Extended family repository/service with member add/list operations.
- Added API endpoints:
  - `POST /api/v1/families/{familyId}/members`
  - `GET /api/v1/families/{familyId}/members`
- Added member role parsing and duplicate Keycloak user checks.
- Added application tests for member add success and invalid role rejection.
- Updated README versioned endpoint examples.

## Files Changed
- src/DragonEnvelopes.Api/Program.cs
- src/DragonEnvelopes.Application/Interfaces/IFamilyRepository.cs
- src/DragonEnvelopes.Application/Services/IFamilyService.cs
- src/DragonEnvelopes.Application/Services/FamilyService.cs
- src/DragonEnvelopes.Infrastructure/Repositories/FamilyRepository.cs
- tests/DragonEnvelopes.Application.Tests/FamilyServiceTests.cs
- README.md

## Validation
- `dotnet build DragonEnvelopes.sln --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release --no-build`
