# 2026-03-06 - Issue #163 Provider secret rewrap endpoint

## Summary
Added a family-scoped security operation to rewrap provider-encrypted fields to the currently active encryption key, enabling deterministic key rotation completion.

## API
New endpoint:
- `POST /api/v1/families/{familyId}/financial/security/rewrap-provider-secrets`

Behavior:
- Requires authenticated family access.
- Runs rewrap using existing repository encryption path.
- Idempotent: safe to call repeatedly.
- Returns summary metadata:
  - `FamilyId`
  - `ProfileFound`
  - `FieldsTouched`
  - `ExecutedAtUtc`

## Application
- Added `RewrapProviderSecretsAsync` to financial integration service interface and implementation.
- Added `ProviderSecretsRewrapDetails` DTO.

## Contracts
- Added `RewrapProviderSecretsResponse` contract.

## Tests
- Added integration coverage for:
  - successful own-family rewrap with encrypted-at-rest verification
  - forbidden cross-family rewrap attempt

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
