# 2026-03-05 - transaction create/list endpoints with optional envelope linkage

## Summary
- Added transaction repository abstraction + EF implementation.
- Added transaction application service for create/list workflows.
- Added API endpoints:
  - `POST /api/v1/transactions`
  - `GET /api/v1/transactions?accountId={accountId}`
- Implemented optional envelope linkage validation (`EnvelopeId` must exist when provided).
- Added a guarded behavior for splits: requests with splits are rejected for now because split persistence is not yet implemented (tracked by later split story).
- Added transaction service unit tests for create/list and validation errors.
- Updated README endpoint list.

## Files Changed
- src/DragonEnvelopes.Application/DTOs/TransactionDetails.cs
- src/DragonEnvelopes.Application/Interfaces/ITransactionRepository.cs
- src/DragonEnvelopes.Application/Services/ITransactionService.cs
- src/DragonEnvelopes.Application/Services/TransactionService.cs
- src/DragonEnvelopes.Application/DependencyInjection.cs
- src/DragonEnvelopes.Infrastructure/Repositories/TransactionRepository.cs
- src/DragonEnvelopes.Infrastructure/DependencyInjection.cs
- src/DragonEnvelopes.Api/Program.cs
- tests/DragonEnvelopes.Application.Tests/TransactionServiceTests.cs
- README.md

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release`
- `docker compose up -d --build api`
- End-to-end: onboard family -> authenticate -> create account -> create transaction -> list transactions by account id
