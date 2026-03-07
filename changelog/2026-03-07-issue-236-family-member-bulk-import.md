# Issue 236 - Family Member Bulk Import (CSV)

## Summary
Implemented family member CSV bulk import with preview + commit endpoints and desktop Family workspace UI support.

## Delivered
- Added application service for member import parsing/validation/dedupe:
  - `IFamilyMemberImportService`
  - `FamilyMemberImportService`
- Added DTOs for preview/commit details:
  - `FamilyMemberImportPreviewDetails`
  - `FamilyMemberImportPreviewRowDetails`
  - `FamilyMemberImportCommitDetails`
- Added family import contracts:
  - `FamilyMemberImportPreviewRequest/Response`
  - `FamilyMemberImportCommitRequest/Response`
- Added API endpoints in both hosts:
  - `POST /api/v1/families/{familyId}/members/import/preview`
  - `POST /api/v1/families/{familyId}/members/import/commit`
- Added validators for family member import request payloads in both API hosts.
- Added endpoint mapper functions for family member import responses.
- Registered new import service in application dependency injection.
- Desktop Family workspace updates:
  - CSV file browse/load support
  - inline CSV content editor
  - preview command + preview grid (row-level errors + duplicate flags)
  - commit command + commit summary
- Desktop service contract and implementation updates for preview/commit APIs.
- Added desktop test coverage for preview/commit workflow behavior.
- Added application unit tests for duplicate detection and commit insertion behavior.
- Added API and Family API integration tests for own-family import and cross-family denial.

## Validation
- `dotnet build DragonEnvelopes.sln -v minimal`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj -v minimal --no-build --filter "FullyQualifiedName~FamilyMemberImportServiceTests"`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal --no-build`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -v minimal --no-build --filter "FullyQualifiedName~FamilyMember_Csv_Import|FullyQualifiedName~Import_FamilyMembers"`
- `dotnet test tests/DragonEnvelopes.Family.Api.IntegrationTests/DragonEnvelopes.Family.Api.IntegrationTests.csproj -v minimal --filter "FullyQualifiedName~Preview_FamilyMember_Import"`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj -v minimal --no-build`
- `dotnet test tests/DragonEnvelopes.Family.Api.IntegrationTests/DragonEnvelopes.Family.Api.IntegrationTests.csproj -v minimal --no-build`
