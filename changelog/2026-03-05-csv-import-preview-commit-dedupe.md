# 2026-03-05 - CSV Transaction Import (Preview + Commit + Dedupe) (#81)

## Summary
- Added import contracts under `src/DragonEnvelopes.Contracts/Imports`:
  - `ImportPreviewRequest`
  - `ImportPreviewResponse`
  - `ImportCommitRequest`
  - `ImportCommitResponse`
- Added import application DTOs and services:
  - `IImportService` / `ImportService`
  - `IImportDedupService` / `ImportDedupService`
- Added API endpoints:
  - `POST /api/v1/imports/transactions/preview`
  - `POST /api/v1/imports/transactions/commit`
- Added deterministic dedupe key strategy:
  - `(AccountId, OccurredAt.Date, Amount, MerchantNormalized, DescriptionNormalized)`
- Added robust CSV parsing behavior:
  - configurable delimiter
  - quoted fields handling
  - required column resolution with alias support and optional header mappings
- Added commit semantics:
  - inserts only accepted rows that are valid and not deduped
  - returns `parsed`, `valid`, `deduped`, `inserted`, `failed`
- Extended transaction repository with:
  - account-family ownership check
  - bulk transaction insert for import commit
- Added request validators for import preview/commit payloads.
- Updated README endpoint inventory.

## Tests Added
- `ImportServiceTests` covering:
  - malformed row validation
  - alternate column naming via header mappings
  - dedupe true-positive and false-positive guard behavior

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release`
- `docker compose up -d --build api`
- Authenticated API smoke validated preview/commit counts for malformed + duplicate + accepted rows.
