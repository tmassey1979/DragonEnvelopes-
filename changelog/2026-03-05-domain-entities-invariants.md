# 2026-03-05 - Domain Entities and Invariants

## Summary

Implemented the core domain model with explicit guard clauses and value objects for monetary, role, month, and email semantics.

## Completed Story

- #43 Implement domain entities with invariants and value-object boundaries

## Key Changes

- Added domain validation exception:
  - `DomainValidationException`
- Added value objects/enums:
  - `Money`
  - `EmailAddress`
  - `BudgetMonth`
  - `TransactionSplit`
  - `MemberRole`
  - `AccountType`
- Added entities with explicit behavior methods and invariant checks:
  - `Family`
  - `FamilyMember`
  - `Account`
  - `Envelope`
  - `Transaction`
  - `Budget`
- Added domain tests covering key invariants:
  - duplicate family member keycloak id rejection
  - account insufficient-balance withdrawal rejection
  - envelope overspend rejection
  - transaction split sum validation
  - budget over-allocation rejection

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`

