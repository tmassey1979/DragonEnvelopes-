# 2026-03-05 - EF Core DbContext and Entity Configurations

## Summary

Implemented `DragonEnvelopesDbContext` and explicit EF Core mappings for all required Phase 1 tables with snake_case naming, required keys/FKs, and value object conversions.

## Completed Story

- #44 Configure PayByDayDbContext and EF Core entity mappings

## Key Changes

- Added DbContext:
  - `src/DragonEnvelopes.Infrastructure/Persistence/DragonEnvelopesDbContext.cs`
- Added entity configuration classes:
  - `FamilyConfiguration` -> `families`
  - `FamilyMemberConfiguration` -> `family_members`
  - `AccountConfiguration` -> `accounts`
  - `EnvelopeConfiguration` -> `envelopes`
  - `TransactionConfiguration` -> `transactions`
  - `BudgetConfiguration` -> `budgets`
- Configured:
  - primary keys and required fields
  - relationship foreign keys and delete behavior
  - decimal precision for money fields (`18,2`)
  - conversions for value objects (`Money`, `EmailAddress`, `BudgetMonth`)
- Updated infrastructure DI registration:
  - registers `DragonEnvelopesDbContext` with PostgreSQL using `ConnectionStrings:Default`
- Added EF Core and Npgsql packages in Infrastructure project.

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
- `./eng/verify-architecture.ps1 -RepoRoot .`

