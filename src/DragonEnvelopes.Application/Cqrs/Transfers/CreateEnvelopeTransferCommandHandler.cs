using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Transfers;

public sealed class CreateEnvelopeTransferCommandHandler(
    IEnvelopeTransferService envelopeTransferService) : ICommandHandler<CreateEnvelopeTransferCommand, EnvelopeTransferDetails>
{
    public Task<EnvelopeTransferDetails> HandleAsync(
        CreateEnvelopeTransferCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopeTransferService.CreateAsync(
            command.FamilyId,
            command.AccountId,
            command.FromEnvelopeId,
            command.ToEnvelopeId,
            command.Amount,
            command.OccurredAt,
            command.Notes,
            cancellationToken);
    }
}
