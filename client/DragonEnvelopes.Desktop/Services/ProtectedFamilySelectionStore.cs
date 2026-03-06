using System.IO;
using System.Text.Json;

namespace DragonEnvelopes.Desktop.Services;

public sealed class ProtectedFamilySelectionStore : IFamilySelectionStore
{
    private readonly string _selectionPath;

    public ProtectedFamilySelectionStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DragonEnvelopes");
        Directory.CreateDirectory(root);
        _selectionPath = Path.Combine(root, "family-selection.json");
    }

    public async Task<Guid?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_selectionPath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_selectionPath, cancellationToken);
            return Guid.TryParse(json.Trim(), out var parsed)
                ? parsed
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public Task SaveAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return File.WriteAllTextAsync(_selectionPath, familyId.ToString("D"), cancellationToken);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_selectionPath))
        {
            File.Delete(_selectionPath);
        }

        return Task.CompletedTask;
    }
}
