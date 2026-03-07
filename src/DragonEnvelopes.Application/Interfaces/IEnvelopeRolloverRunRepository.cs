using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IEnvelopeRolloverRunRepository
{
    Task<EnvelopeRolloverRun?> GetByFamilyAndMonthAsync(
        Guid familyId,
        string month,
        CancellationToken cancellationToken = default);

    Task AddAsync(EnvelopeRolloverRun run, CancellationToken cancellationToken = default);
}
