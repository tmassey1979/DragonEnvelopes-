# Provider Secret Encryption and Key Rotation

## Scope
DragonEnvelopes encrypts provider-sensitive values at rest in `family_financial_profiles`:
- `PlaidAccessToken`
- `StripeCustomerId`
- `StripeDefaultPaymentMethodId`

Values are stored with envelope format:
`enc:v1:<keyId>:<nonceBase64>:<ciphertextBase64>:<tagBase64>`

## Configuration
Use the `ProviderSecretEncryption` section:

```json
"ProviderSecretEncryption": {
  "Enabled": true,
  "ActiveKeyId": "key-2026-03",
  "Keys": {
    "key-2026-03": "<base64-32-byte-key>",
    "key-2025-12": "<base64-32-byte-key>"
  }
}
```

Rules:
- Keys must decode to **32 bytes** (AES-256).
- `ActiveKeyId` must exist in `Keys` when encryption is enabled.
- Keep prior keys available until all required legacy values are re-encrypted.

## Rotation Procedure
1. Generate a new 32-byte key and base64 encode it.
2. Add the new key under `ProviderSecretEncryption:Keys`.
3. Set `ProviderSecretEncryption:ActiveKeyId` to the new key id.
4. Deploy/restart API instances.
5. Allow normal write activity to re-encrypt touched profiles with the new active key.
6. After confirmation no required records depend on old keys, remove retired keys.

## Operational Notes
- Existing plaintext values are supported and are encrypted on subsequent writes.
- Existing encrypted values are decrypted using the key id embedded in the payload.
- If a required decryption key is missing, provider operations for affected profiles fail fast.
