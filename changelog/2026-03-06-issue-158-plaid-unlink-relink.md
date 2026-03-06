# 2026-03-06 - Issue #158 Plaid unlink/relink workflow

## Summary
Implemented family-scoped Plaid account link unlink support across API, application/infrastructure, and desktop UI. Existing upsert behavior continues to support relink by re-saving a link for the same account.

## Backend
- Added repository methods to fetch a Plaid link by `(familyId, linkId)` for update and to delete links.
- Added service method `DeleteAccountLinkAsync(familyId, linkId)` with family-bound validation.
- Added API endpoint:
  - `DELETE /api/v1/families/{familyId}/financial/plaid/account-links/{linkId}`
- Added structured API log for link deletion with family/link/user identifiers.

## Desktop
- Extended financial integration data service with `DeletePlaidAccountLinkAsync`.
- Added `SelectedPlaidAccountLink` state and delete command in financial integrations view model.
- Added delete action in Plaid section of the financial integrations template.
- Preserved selected link across link reloads to keep the grid interaction stable.

## Tests
- Added auth/isolation integration coverage:
  - user A can delete own-family Plaid link
  - user A cannot delete family B Plaid link
- Seeded deterministic Plaid links for both test families.
- Verified existing desktop smoke tests continue to pass.

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --no-build`
