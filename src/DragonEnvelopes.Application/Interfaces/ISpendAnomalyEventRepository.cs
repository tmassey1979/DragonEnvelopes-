using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface ISpendAnomalyEventRepository
{
    Task<bool> ExistsForTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        SpendAnomalyEvent anomalyEvent,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpendAnomalyEvent>> ListByFamilyAsync(
        Guid familyId,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpendAnomalySample>> ListRecentSpendSamplesAsync(
        Guid familyId,
        DateTimeOffset occurredSinceUtc,
        Guid? excludeTransactionId,
        int take,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
