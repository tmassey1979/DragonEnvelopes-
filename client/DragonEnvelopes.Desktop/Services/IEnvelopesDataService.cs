using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public interface IEnvelopesDataService
{
    Task<IReadOnlyList<EnvelopeListItemViewModel>> GetEnvelopesAsync(CancellationToken cancellationToken = default);

    Task<EnvelopeListItemViewModel> CreateEnvelopeAsync(
        string name,
        decimal monthlyBudget,
        CancellationToken cancellationToken = default);

    Task<EnvelopeListItemViewModel> UpdateEnvelopeAsync(
        Guid envelopeId,
        string name,
        decimal monthlyBudget,
        bool isArchived,
        CancellationToken cancellationToken = default);

    Task<EnvelopeListItemViewModel> ArchiveEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken = default);
}
