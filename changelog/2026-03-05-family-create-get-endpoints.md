# 2026-03-05 - family create get endpoints

## Summary
- Added application family service and repository abstraction for create/get workflows.
- Added infrastructure EF repository implementation for families and family members.
- Added API endpoints:
  - `POST /api/v1/families`
  - `GET /api/v1/families/{familyId}`
- Added application tests for family service create behavior and duplicate-name rejection.
- Updated README versioned endpoint examples.

## Files Changed
- src/DragonEnvelopes.Api/Program.cs
- src/DragonEnvelopes.Application/DependencyInjection.cs
- src/DragonEnvelopes.Application/DTOs/FamilyDetails.cs
- src/DragonEnvelopes.Application/Interfaces/IFamilyRepository.cs
- src/DragonEnvelopes.Application/Services/IFamilyService.cs
- src/DragonEnvelopes.Application/Services/FamilyService.cs
- src/DragonEnvelopes.Infrastructure/DependencyInjection.cs
- src/DragonEnvelopes.Infrastructure/Repositories/FamilyRepository.cs
- tests/DragonEnvelopes.Application.Tests/FamilyServiceTests.cs
- README.md

## Validation
- `dotnet build DragonEnvelopes.sln --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release --no-build`
