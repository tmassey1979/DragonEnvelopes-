using System.Security.Cryptography;
using System.Text;
using DragonEnvelopes.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Infrastructure.Services;

public sealed class ProviderSecretEncryptionOptions
{
    public bool Enabled { get; init; } = true;

    public string ActiveKeyId { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Keys { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
}

public sealed class ProviderSecretProtector : IProviderSecretProtector
{
    private const string Prefix = "enc:v1:";
    private static readonly byte[] AssociatedData = Encoding.UTF8.GetBytes("dragonenvelopes-provider-secret-v1");

    private readonly bool _enabled;
    private readonly string _activeKeyId;
    private readonly IReadOnlyDictionary<string, byte[]> _keyRing;

    public ProviderSecretProtector(IOptions<ProviderSecretEncryptionOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;

        _enabled = options.Enabled;
        _activeKeyId = options.ActiveKeyId.Trim();
        _keyRing = options.Keys
            .Where(static entry => !string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value))
            .ToDictionary(
                static entry => entry.Key.Trim(),
                static entry => DecodeKey(entry.Key.Trim(), entry.Value.Trim()),
                StringComparer.Ordinal);

        if (_enabled)
        {
            if (string.IsNullOrWhiteSpace(_activeKeyId))
            {
                throw new InvalidOperationException("Provider secret encryption active key id must be configured when encryption is enabled.");
            }

            if (!_keyRing.ContainsKey(_activeKeyId))
            {
                throw new InvalidOperationException($"Provider secret encryption key '{_activeKeyId}' was not found in configured key ring.");
            }
        }
    }

    public string Protect(string value)
    {
        var normalized = NormalizeRequired(value);
        if (!_enabled || IsProtected(normalized))
        {
            return normalized;
        }

        var key = _keyRing[_activeKeyId];
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(normalized);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key, tagSizeInBytes: 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, AssociatedData);

        return $"{Prefix}{_activeKeyId}:{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(ciphertext)}:{Convert.ToBase64String(tag)}";
    }

    public string Unprotect(string value)
    {
        var normalized = NormalizeRequired(value);
        if (!IsProtected(normalized))
        {
            return normalized;
        }

        var parts = normalized.Split(':');
        if (parts.Length != 6 || !parts[0].Equals("enc", StringComparison.Ordinal) || !parts[1].Equals("v1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Encrypted provider secret payload format is invalid.");
        }

        var keyId = parts[2];
        if (!_keyRing.TryGetValue(keyId, out var key))
        {
            throw new InvalidOperationException($"Provider secret encryption key '{keyId}' is unavailable for decryption.");
        }

        try
        {
            var nonce = Convert.FromBase64String(parts[3]);
            var ciphertext = Convert.FromBase64String(parts[4]);
            var tag = Convert.FromBase64String(parts[5]);

            var plaintext = new byte[ciphertext.Length];
            using var aes = new AesGcm(key, tagSizeInBytes: 16);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, AssociatedData);
            return Encoding.UTF8.GetString(plaintext);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Encrypted provider secret payload is not valid base64.", ex);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Provider secret decryption failed.", ex);
        }
    }

    public bool IsProtected(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.StartsWith(Prefix, StringComparison.Ordinal);
    }

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Provider secret value is required.");
        }

        return value.Trim();
    }

    private static byte[] DecodeKey(string keyId, string encodedValue)
    {
        try
        {
            var key = Convert.FromBase64String(encodedValue);
            if (key.Length != 32)
            {
                throw new InvalidOperationException(
                    $"Provider secret encryption key '{keyId}' must decode to 32 bytes for AES-256.");
            }

            return key;
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"Provider secret encryption key '{keyId}' is not valid base64.",
                ex);
        }
    }
}
