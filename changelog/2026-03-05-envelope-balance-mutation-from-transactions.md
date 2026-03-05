# 2026-03-05 - envelope balance mutation from transaction events

## Summary
- Updated transaction create flow to mutate linked envelope balances when `EnvelopeId` is provided.
- Behavior implemented:
  - Positive transaction amount allocates to envelope balance.
  - Negative transaction amount spends from envelope balance.
- Mutation occurs in the same DbContext scope before transaction persistence save to keep consistency.
- Added unit tests for both allocation and spend mutation behavior.

## Files Changed
- src/DragonEnvelopes.Application/Services/TransactionService.cs
- tests/DragonEnvelopes.Application.Tests/TransactionServiceTests.cs

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release`
- `docker compose up -d --build api`
- End-to-end smoke: create envelope -> post +120 transaction with envelopeId -> balance 120 -> post -30 transaction with envelopeId -> balance 90
