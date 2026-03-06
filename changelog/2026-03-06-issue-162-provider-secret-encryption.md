# 2026-03-06 - Issue #162 Provider secret encryption at rest

## Summary
Implemented encryption at rest for provider-sensitive tokens/identifiers stored in family financial profiles, with key-ring support and key rotation guidance.

## Encryption coverage
Applied transparent at-rest protection for:
- `PlaidAccessToken`
- `StripeCustomerId`
- `StripeDefaultPaymentMethodId`

## Implementation details
- Added `IProviderSecretProtector` abstraction.
- Added `ProviderSecretProtector` (AES-256-GCM) with envelope format:
  - `enc:v1:<keyId>:<nonce>:<ciphertext>:<tag>`
- Added `ProviderSecretEncryptionOptions` key-ring model:
  - `Enabled`
  - `ActiveKeyId`
  - `Keys` (base64-encoded 32-byte keys)
- Wired protector in infrastructure DI and configuration parsing.
- Updated `FamilyFinancialProfileRepository` to:
  - decrypt values on read
  - encrypt values before save
  - migrate legacy plaintext values to encrypted format on subsequent saves

## Configuration
Added `ProviderSecretEncryption` section to API appsettings with development key defaults for local/dev usage.

## Documentation
Added rotation/operations guide:
- `docs/operations/provider-secret-key-rotation.md`

## Tests
Added `ProviderSecretEncryptionTests` covering:
- protector round-trip encryption/decryption
- key rotation compatibility across old/new key ids
- repository encryption on save and transparent decryption on read
- legacy plaintext migration to encrypted storage

## Validation
- `dotnet build DragonEnvelopes.sln`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj`
- `dotnet test tests/DragonEnvelopes.Api.IntegrationTests/DragonEnvelopes.Api.IntegrationTests.csproj`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj`
