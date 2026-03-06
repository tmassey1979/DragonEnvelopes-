# Issue #129 - Microservice Boundaries ADR

## Summary
Added an accepted architecture decision record defining service boundaries, data ownership, auth strategy, migration sequencing, and rollout/rollback gates for microservice extraction.

## Artifacts
- `docs/adr/0001-microservice-boundaries-and-migration-plan.md`
- `docs/adr/README.md`

## Decision Highlights
- Extraction order locked: `family-service` first, `ledger-service` second.
- Data ownership split defined for family and ledger datastores.
- Internal family-access auth contract defined for downstream service authorization.
- Phase gates and rollback strategy documented.

## Validation
- Documentation change validated by repository review and issue linkage updates.

## Related
- Parent tracker: `#132`
- Execution stories: `#130`, `#131`
