using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Interfaces;

public interface IReportingRepository
{
    Task<IReadOnlyList<EnvelopeBalanceReportDetails>> ListEnvelopeBalancesAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionReportRow>> ListTransactionsAsync(
        Guid familyId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toInclusive,
        CancellationToken cancellationToken = default);
}

public sealed record TransactionReportRow(
    decimal Amount,
    string? Category,
    DateTimeOffset OccurredAt);
