using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IEnvelopeTransferService
{
    Task<EnvelopeTransferDetails> CreateAsync(
        Guid familyId,
        Guid accountId,
        Guid fromEnvelopeId,
        Guid toEnvelopeId,
        decimal amount,
        DateTimeOffset occurredAt,
        string? notes,
        CancellationToken cancellationToken = default);
}
