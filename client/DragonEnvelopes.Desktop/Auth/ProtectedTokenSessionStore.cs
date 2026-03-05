using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO;

namespace DragonEnvelopes.Desktop.Auth;

public sealed class ProtectedTokenSessionStore : IAuthSessionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly string _sessionFilePath;

    public ProtectedTokenSessionStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DragonEnvelopes");

        Directory.CreateDirectory(root);
        _sessionFilePath = Path.Combine(root, "session.dat");
    }

    public async Task<AuthSession?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_sessionFilePath))
        {
            return null;
        }

        try
        {
            var encrypted = await File.ReadAllBytesAsync(_sessionFilePath, cancellationToken);
            var decrypted = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(decrypted);
            return JsonSerializer.Deserialize<AuthSession>(json, SerializerOptions);
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task SaveAsync(AuthSession session, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(session, SerializerOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var encrypted = ProtectedData.Protect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(_sessionFilePath, encrypted, cancellationToken);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_sessionFilePath))
        {
            File.Delete(_sessionFilePath);
        }

        return Task.CompletedTask;
    }
}
