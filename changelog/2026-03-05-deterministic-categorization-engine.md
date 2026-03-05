# 2026-03-05 - Deterministic Categorization Engine (#77)

## Summary
- Added `ICategorizationRuleEngine` and `CategorizationRuleEngine` to evaluate enabled categorization rules.
- Implemented condition DSL support for:
  - `merchantContains`
  - `descriptionContains`
  - `amountMin`
  - `amountMax`
  - `categoryIsNull`
- Implemented action DSL support for:
  - `setCategory`
- Enforced deterministic precedence in engine evaluation:
  - `Priority ASC`, then `CreatedAt ASC`, first-match wins.
- Integrated transaction create flow with categorization engine:
  - when incoming category is null/blank, evaluate rules and apply matched category.
  - when incoming category is explicitly set, skip automation and preserve explicit value.
- Extended transaction repository with account->family lookup required for family-scoped rule evaluation.

## Tests Added/Updated
- `CategorizationRuleEngineTests`:
  - first-match precedence by priority
  - tie-break by created date
  - no-match behavior
- `TransactionServiceTests`:
  - auto category assignment when category omitted
  - explicit category bypasses engine
  - updated existing tests for new service dependency and account family lookup

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release`
- `docker compose up -d --build api`
- Manual API smoke:
  - onboard + auth + account create
  - automation rule create
  - transaction create without category -> category auto-applied
  - transaction create with explicit category -> explicit category preserved
