using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Planning;

public sealed record CreateEnvelopeGoalCommand(
    Guid FamilyId,
    Guid EnvelopeId,
    decimal TargetAmount,
    DateOnly DueDate,
    string Status) : ICommand<EnvelopeGoalDetails>;

public sealed class CreateEnvelopeGoalCommandHandler(
    IEnvelopeGoalService envelopeGoalService) : ICommandHandler<CreateEnvelopeGoalCommand, EnvelopeGoalDetails>
{
    public Task<EnvelopeGoalDetails> HandleAsync(
        CreateEnvelopeGoalCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopeGoalService.CreateAsync(
            command.FamilyId,
            command.EnvelopeId,
            command.TargetAmount,
            command.DueDate,
            command.Status,
            cancellationToken);
    }
}

public sealed record ListEnvelopeGoalsByFamilyQuery(Guid FamilyId) : IQuery<IReadOnlyList<EnvelopeGoalDetails>>;

public sealed class ListEnvelopeGoalsByFamilyQueryHandler(
    IEnvelopeGoalService envelopeGoalService) : IQueryHandler<ListEnvelopeGoalsByFamilyQuery, IReadOnlyList<EnvelopeGoalDetails>>
{
    public Task<IReadOnlyList<EnvelopeGoalDetails>> HandleAsync(
        ListEnvelopeGoalsByFamilyQuery query,
        CancellationToken cancellationToken = default)
    {
        return envelopeGoalService.ListByFamilyAsync(query.FamilyId, cancellationToken);
    }
}

public sealed record GetEnvelopeGoalByIdQuery(Guid GoalId) : IQuery<EnvelopeGoalDetails?>;

public sealed class GetEnvelopeGoalByIdQueryHandler(
    IEnvelopeGoalService envelopeGoalService) : IQueryHandler<GetEnvelopeGoalByIdQuery, EnvelopeGoalDetails?>
{
    public Task<EnvelopeGoalDetails?> HandleAsync(
        GetEnvelopeGoalByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return envelopeGoalService.GetByIdAsync(query.GoalId, cancellationToken);
    }
}

public sealed record UpdateEnvelopeGoalCommand(
    Guid GoalId,
    decimal TargetAmount,
    DateOnly DueDate,
    string Status) : ICommand<EnvelopeGoalDetails>;

public sealed class UpdateEnvelopeGoalCommandHandler(
    IEnvelopeGoalService envelopeGoalService) : ICommandHandler<UpdateEnvelopeGoalCommand, EnvelopeGoalDetails>
{
    public Task<EnvelopeGoalDetails> HandleAsync(
        UpdateEnvelopeGoalCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopeGoalService.UpdateAsync(
            command.GoalId,
            command.TargetAmount,
            command.DueDate,
            command.Status,
            cancellationToken);
    }
}

public sealed record DeleteEnvelopeGoalCommand(Guid GoalId) : ICommand<bool>;

public sealed class DeleteEnvelopeGoalCommandHandler(
    IEnvelopeGoalService envelopeGoalService) : ICommandHandler<DeleteEnvelopeGoalCommand, bool>
{
    public async Task<bool> HandleAsync(
        DeleteEnvelopeGoalCommand command,
        CancellationToken cancellationToken = default)
    {
        await envelopeGoalService.DeleteAsync(command.GoalId, cancellationToken);
        return true;
    }
}

public sealed record ProjectEnvelopeGoalsQuery(
    Guid FamilyId,
    DateOnly AsOf) : IQuery<IReadOnlyList<EnvelopeGoalProjectionDetails>>;

public sealed class ProjectEnvelopeGoalsQueryHandler(
    IEnvelopeGoalService envelopeGoalService) : IQueryHandler<ProjectEnvelopeGoalsQuery, IReadOnlyList<EnvelopeGoalProjectionDetails>>
{
    public Task<IReadOnlyList<EnvelopeGoalProjectionDetails>> HandleAsync(
        ProjectEnvelopeGoalsQuery query,
        CancellationToken cancellationToken = default)
    {
        return envelopeGoalService.ProjectAsync(
            query.FamilyId,
            query.AsOf,
            cancellationToken);
    }
}
