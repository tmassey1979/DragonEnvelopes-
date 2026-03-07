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

    Task CreateGoalAsync(
        Guid envelopeId,
        decimal targetAmount,
        DateOnly dueDate,
        string status,
        CancellationToken cancellationToken = default);

    Task UpdateGoalAsync(
        Guid goalId,
        decimal targetAmount,
        DateOnly dueDate,
        string status,
        CancellationToken cancellationToken = default);

    Task DeleteGoalAsync(Guid goalId, CancellationToken cancellationToken = default);
}
