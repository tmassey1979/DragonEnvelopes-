# 2026-03-05 - API Versioning and OpenAPI Auth Documentation

## Summary

Added URL-segment API versioning (`/api/v{version}`), configured Swagger bearer authentication documentation, and documented versioning/error contracts in README.

## Completed Story

- #85 Configure API versioning strategy and OpenAPI auth documentation

## Key Changes

- Added API versioning package:
  - `src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj`
- Configured API versioning strategy:
  - URL segment mode with default version `1.0`
  - Route group uses `/api/v{version:apiVersion}` with explicit v1 mapping
  - `src/DragonEnvelopes.Api/Program.cs`
- Updated Swagger/OpenAPI configuration:
  - Added v1 doc metadata
  - Added bearer security definition with token usage example
  - Added operation filter to apply security requirements to authorized endpoints and annotate `401/403`
  - `src/DragonEnvelopes.Api/Program.cs`
  - `src/DragonEnvelopes.Api/CrossCutting/OpenApi/BearerSecurityOperationFilter.cs`
- Updated route mappings to versioned API group:
  - `GET /api/v1/weatherforecast`
  - `GET /api/v1/auth/me`
  - `GET /api/v1/auth/parent-only`
  - `src/DragonEnvelopes.Api/Program.cs`
- Updated project documentation:
  - Versioning approach and error contract details in README
  - `README.md`

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
