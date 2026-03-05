# 2026-03-05 - DI Composition Root Modules

## Summary

Implemented centralized dependency registration modules in Application and Infrastructure, and wired API startup to use those composition root methods.

## Completed Story

- #40 Register DI composition root modules in API startup

## Key Changes

- Added Application DI module:
  - [src/DragonEnvelopes.Application/DependencyInjection.cs](../src/DragonEnvelopes.Application/DependencyInjection.cs)
- Added Infrastructure DI module:
  - [src/DragonEnvelopes.Infrastructure/DependencyInjection.cs](../src/DragonEnvelopes.Infrastructure/DependencyInjection.cs)
- Added baseline service/mapper/repository registrations:
  - `IHealthPingService` -> `HealthPingService`
  - `IApplicationMapper` -> `IdentityMapper`
  - `IClock` -> `SystemClock`
  - `IRepositoryMarker` -> `RepositoryMarker`
- Wired API startup to module methods in:
  - [src/DragonEnvelopes.Api/Program.cs](../src/DragonEnvelopes.Api/Program.cs)
- Added required DI/config abstraction package references for Application and Infrastructure projects.

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`

