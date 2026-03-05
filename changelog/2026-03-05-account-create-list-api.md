# 2026-03-05 - account create/list API and application service slice

## Summary
- Added account repository abstraction and infrastructure implementation for create/list operations.
- Added application account service with account type parsing, duplicate-name checks, and family existence checks.
- Added API endpoints:
  - `POST /api/v1/accounts`
  - `GET /api/v1/accounts?familyId={familyId}` (familyId optional)
- Added account service unit tests for create/list and validation failures.
- Updated API endpoint docs in README.

## Files Changed
- src/DragonEnvelopes.Application/DTOs/AccountDetails.cs
- src/DragonEnvelopes.Application/Interfaces/IAccountRepository.cs
- src/DragonEnvelopes.Application/Services/IAccountService.cs
- src/DragonEnvelopes.Application/Services/AccountService.cs
- src/DragonEnvelopes.Application/DependencyInjection.cs
- src/DragonEnvelopes.Infrastructure/Repositories/AccountRepository.cs
- src/DragonEnvelopes.Infrastructure/DependencyInjection.cs
- src/DragonEnvelopes.Api/Program.cs
- tests/DragonEnvelopes.Application.Tests/AccountServiceTests.cs
- README.md

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release`
- `docker compose up -d --build api`
- End-to-end: onboard family -> login token -> create account -> list accounts by family id
