using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Planning;

public sealed record CreateEnvelopeCommand(
    Guid FamilyId,
    string Name,
    decimal MonthlyBudget,
    string? RolloverMode,
    decimal? RolloverCap) : ICommand<EnvelopeDetails>;

public sealed class CreateEnvelopeCommandHandler(
    IEnvelopeService envelopeService) : ICommandHandler<CreateEnvelopeCommand, EnvelopeDetails>
{
    public Task<EnvelopeDetails> HandleAsync(
        CreateEnvelopeCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopeService.CreateAsync(
            command.FamilyId,
            command.Name,
            command.MonthlyBudget,
            command.RolloverMode,
            command.RolloverCap,
            cancellationToken);
    }
}

public sealed record GetEnvelopeByIdQuery(Guid EnvelopeId) : IQuery<EnvelopeDetails?>;

public sealed class GetEnvelopeByIdQueryHandler(
    IEnvelopeService envelopeService) : IQueryHandler<GetEnvelopeByIdQuery, EnvelopeDetails?>
{
    public Task<EnvelopeDetails?> HandleAsync(
        GetEnvelopeByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return envelopeService.GetByIdAsync(query.EnvelopeId, cancellationToken);
    }
}

public sealed record ListEnvelopesByFamilyQuery(Guid FamilyId) : IQuery<IReadOnlyList<EnvelopeDetails>>;

public sealed class ListEnvelopesByFamilyQueryHandler(
    IEnvelopeService envelopeService) : IQueryHandler<ListEnvelopesByFamilyQuery, IReadOnlyList<EnvelopeDetails>>
{
    public Task<IReadOnlyList<EnvelopeDetails>> HandleAsync(
        ListEnvelopesByFamilyQuery query,
        CancellationToken cancellationToken = default)
    {
        return envelopeService.ListByFamilyAsync(query.FamilyId, cancellationToken);
    }
}

public sealed record UpdateEnvelopeCommand(
    Guid EnvelopeId,
    string Name,
    decimal MonthlyBudget,
    bool IsArchived,
    string? RolloverMode,
    decimal? RolloverCap) : ICommand<EnvelopeDetails>;

public sealed class UpdateEnvelopeCommandHandler(
    IEnvelopeService envelopeService) : ICommandHandler<UpdateEnvelopeCommand, EnvelopeDetails>
{
    public Task<EnvelopeDetails> HandleAsync(
        UpdateEnvelopeCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopeService.UpdateAsync(
            command.EnvelopeId,
            command.Name,
            command.MonthlyBudget,
            command.IsArchived,
            command.RolloverMode,
            command.RolloverCap,
            cancellationToken);
    }
}

public sealed record UpdateEnvelopeRolloverPolicyCommand(
    Guid EnvelopeId,
    string RolloverMode,
    decimal? RolloverCap) : ICommand<EnvelopeDetails>;

public sealed class UpdateEnvelopeRolloverPolicyCommandHandler(
    IEnvelopeService envelopeService) : ICommandHandler<UpdateEnvelopeRolloverPolicyCommand, EnvelopeDetails>
{
    public Task<EnvelopeDetails> HandleAsync(
        UpdateEnvelopeRolloverPolicyCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopeService.UpdateRolloverPolicyAsync(
            command.EnvelopeId,
            command.RolloverMode,
            command.RolloverCap,
            cancellationToken);
    }
}

public sealed record ArchiveEnvelopeCommand(Guid EnvelopeId) : ICommand<EnvelopeDetails>;

public sealed class ArchiveEnvelopeCommandHandler(
    IEnvelopeService envelopeService) : ICommandHandler<ArchiveEnvelopeCommand, EnvelopeDetails>
{
    public Task<EnvelopeDetails> HandleAsync(
        ArchiveEnvelopeCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopeService.ArchiveAsync(command.EnvelopeId, cancellationToken);
    }
}
