using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Cqrs.Transfers;

public sealed record CreateEnvelopeTransferCommand(
    Guid FamilyId,
    Guid AccountId,
    Guid FromEnvelopeId,
    Guid ToEnvelopeId,
    decimal Amount,
    DateTimeOffset OccurredAt,
    string? Notes) : ICommand<EnvelopeTransferDetails>;
