# 2026-03-05 - Phase 1 Contract DTO Definitions

## Summary

Added Phase 1 API contract DTOs for families, accounts, envelopes, transactions, and budgets in `DragonEnvelopes.Contracts`, with serialization tests to validate compatibility.

## Completed Story

- #84 Define DTO contracts for all Phase 1 API endpoints

## Key Changes

- Added family DTOs:
  - `CreateFamilyRequest`
  - `AddFamilyMemberRequest`
  - `FamilyMemberResponse`
  - `FamilyResponse`
- Added account DTOs:
  - `CreateAccountRequest`
  - `AccountResponse`
- Added envelope DTOs:
  - `CreateEnvelopeRequest`
  - `UpdateEnvelopeRequest`
  - `EnvelopeResponse`
- Added transaction DTOs:
  - `TransactionSplitRequest`
  - `CreateTransactionRequest`
  - `TransactionSplitResponse`
  - `TransactionResponse`
- Added budget DTOs:
  - `CreateBudgetRequest`
  - `UpdateBudgetRequest`
  - `BudgetResponse`
- Added contract serialization tests:
  - `FamilyResponse` round-trip
  - `TransactionResponse` round-trip
  - `BudgetResponse` round-trip
- Added `DragonEnvelopes.Contracts` reference to application test project.

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`

