using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IEnvelopeGoalService
{
    Task<EnvelopeGoalDetails> CreateAsync(
        Guid familyId,
        Guid envelopeId,
        decimal targetAmount,
        DateOnly dueDate,
        string status,
        CancellationToken cancellationToken = default);

    Task<EnvelopeGoalDetails?> GetByIdAsync(Guid goalId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvelopeGoalDetails>> ListByFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<EnvelopeGoalDetails> UpdateAsync(
        Guid goalId,
        decimal targetAmount,
        DateOnly dueDate,
        string status,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid goalId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvelopeGoalProjectionDetails>> ProjectAsync(
        Guid familyId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);
}
