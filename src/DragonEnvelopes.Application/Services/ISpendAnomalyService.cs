using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface ISpendAnomalyService
{
    Task DetectAndRecordAsync(
        Guid familyId,
        Guid transactionId,
        Guid accountId,
        string merchant,
        decimal amount,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpendAnomalyEventDetails>> ListByFamilyAsync(
        Guid familyId,
        int take,
        CancellationToken cancellationToken = default);
}
