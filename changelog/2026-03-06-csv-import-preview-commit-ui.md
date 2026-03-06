# 2026-03-06 - CSV Import Preview and Commit UI

## Summary
Implemented a dedicated desktop Imports page for CSV transaction preview/commit with dedupe/error feedback.

## Changes
- Added imports data service + interface with API bindings:
  - `GET /accounts?familyId=...`
  - `POST /imports/transactions/preview`
  - `POST /imports/transactions/commit`
- Added import data/result models and preview row viewmodel.
- Added `ImportsViewModel` with:
  - account selection
  - CSV content + delimiter input
  - preview flow and row table rendering
  - commit flow with optional deduped-row inclusion
  - summary stats for preview and commit results
  - loading/error/reset handling
- Added `/imports` route and full XAML template.

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj -p:OutputPath=bin/TempVerify/` (pass)

## Notes
- First version uses `null` header mappings (default parser mapping path).
- Commit accepts only rows that are error-free and (optionally) non-duplicate.
