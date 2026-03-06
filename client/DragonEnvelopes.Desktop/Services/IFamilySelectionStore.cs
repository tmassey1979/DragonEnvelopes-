namespace DragonEnvelopes.Desktop.Services;

public interface IFamilySelectionStore
{
    Task<Guid?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
