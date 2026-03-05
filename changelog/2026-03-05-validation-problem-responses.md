# 2026-03-05 - FluentValidation and Global Problem Responses

## Summary

Integrated FluentValidation into the API pipeline and added centralized RFC7807 problem response handling for validation, domain, payload, and unexpected errors.

## Completed Story

- #46 Integrate FluentValidation and global exception problem responses

## Key Changes

- Added FluentValidation DI integration package:
  - `src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj`
- Updated API composition root:
  - Registers validators from API assembly
  - Configures `AddProblemDetails` with request `instance` and `traceId`
  - Registers a global `IExceptionHandler`
  - Enables `UseExceptionHandler()` in middleware pipeline
  - `src/DragonEnvelopes.Api/Program.cs`
- Added global exception handler:
  - Maps FluentValidation failures to `400` validation problem details
  - Maps domain invariant failures to `422` problem details
  - Maps bad payload/JSON to `400`
  - Maps unhandled failures to `500`
  - `src/DragonEnvelopes.Api/CrossCutting/Errors/GlobalExceptionHandler.cs`
- Added minimal API validation filter factory and extensions:
  - Endpoint filter factory inspects handler parameters and runs registered validators automatically
  - Throws FluentValidation `ValidationException` for centralized handling
  - `src/DragonEnvelopes.Api/CrossCutting/Validation/FluentValidationEndpointFilterFactory.cs`
  - `src/DragonEnvelopes.Api/CrossCutting/Validation/ValidationEndpointExtensions.cs`
- Added request validators for current contract DTOs:
  - Family create/member add
  - Account create
  - Envelope create/update
  - Transaction create/splits (including split total validation)
  - Budget create/update
  - `src/DragonEnvelopes.Api/CrossCutting/Validation/Validators/RequestValidators.cs`

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
