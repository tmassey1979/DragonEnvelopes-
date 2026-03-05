# 2026-03-05 - Income Allocation Engine with Manual Override (#78)

## Summary
- Added `IIncomeAllocationEngine` + `IncomeAllocationEngine` for rules-based allocation on positive transactions.
- Implemented deterministic rule execution: `Priority ASC`, then `CreatedAt ASC`.
- Implemented allocation action DSL v1 parsing:
  - `targetEnvelopeId`
  - `allocationType` (`FixedAmount` | `Percent`)
  - `value`
- Implemented allocation execution behavior:
  - allocate from positive amount only
  - cap each allocation by remaining amount
  - stop when amount is exhausted
  - remainder stays unallocated
- Integrated transaction create path:
  - automatic allocation runs only when transaction is positive, no manual splits are provided, and no single envelope is explicitly set
  - manual splits suppress automatic allocation engine
- Reused split persistence path from existing split transaction support.

## Tests Added/Updated
- `IncomeAllocationEngineTests`:
  - mixed fixed + percent allocation
  - percent rounding behavior
  - capping when requested total exceeds available amount
- `TransactionServiceTests`:
  - auto-allocation generates persisted splits
  - manual splits suppress allocation engine
  - constructor/setup updates for new engine dependency

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release`
- `docker compose up -d --build api`
- Manual API smoke:
  - created allocation rules (fixed + percent)
  - positive transaction auto-generated splits totaling full allocated amount
  - manual split transaction bypassed automatic allocation
