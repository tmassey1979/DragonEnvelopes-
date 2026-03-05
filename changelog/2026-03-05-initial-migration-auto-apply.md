# 2026-03-05 - Initial EF Migration and Startup Auto-Apply

## Summary

Generated the initial EF Core migration and configured API startup to apply pending migrations automatically at boot.

## Completed Story

- #45 Create initial EF migration and startup auto-apply flow

## Key Changes

- Added EF Design package references required for migration tooling:
  - `src/DragonEnvelopes.Infrastructure/DragonEnvelopes.Infrastructure.csproj`
  - `src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj`
- Added initial migration artifacts:
  - `src/DragonEnvelopes.Infrastructure/Persistence/Migrations/20260305164221_InitialCreate.cs`
  - `src/DragonEnvelopes.Infrastructure/Persistence/Migrations/20260305164221_InitialCreate.Designer.cs`
  - `src/DragonEnvelopes.Infrastructure/Persistence/Migrations/DragonEnvelopesDbContextModelSnapshot.cs`
- Updated API startup to apply migrations at launch with explicit logging:
  - `src/DragonEnvelopes.Api/Program.cs`
- Adjusted domain constructor signatures for EF constructor binding compatibility:
  - `Account`
  - `Transaction`

## Validation

- `dotnet ef migrations add InitialCreate ...` completed successfully.
- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
- `docker compose up -d --build` then verified:
  - migration history row inserted
  - tables created in `dragonenvelopes_app`
  - API startup logs show migration apply steps
- `docker compose down`

