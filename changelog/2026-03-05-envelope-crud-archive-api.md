# 2026-03-05 - envelope create/list/get/update/archive API slice

## Summary
- Added envelope repository abstraction and EF implementation.
- Added envelope application service for create/get/list/update/archive workflows.
- Added API endpoints:
  - `POST /api/v1/envelopes`
  - `GET /api/v1/envelopes?familyId={familyId}`
  - `GET /api/v1/envelopes/{envelopeId}`
  - `PUT /api/v1/envelopes/{envelopeId}`
  - `POST /api/v1/envelopes/{envelopeId}/archive`
- Added envelope service unit tests.
- Updated README endpoint documentation.

## Files Changed
- src/DragonEnvelopes.Application/DTOs/EnvelopeDetails.cs
- src/DragonEnvelopes.Application/Interfaces/IEnvelopeRepository.cs
- src/DragonEnvelopes.Application/Services/IEnvelopeService.cs
- src/DragonEnvelopes.Application/Services/EnvelopeService.cs
- src/DragonEnvelopes.Application/DependencyInjection.cs
- src/DragonEnvelopes.Infrastructure/Repositories/EnvelopeRepository.cs
- src/DragonEnvelopes.Infrastructure/DependencyInjection.cs
- src/DragonEnvelopes.Api/Program.cs
- tests/DragonEnvelopes.Application.Tests/EnvelopeServiceTests.cs
- README.md

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release`
- `docker compose up -d --build api`
- End-to-end smoke: onboard -> auth token -> create envelope -> list -> update -> archive
