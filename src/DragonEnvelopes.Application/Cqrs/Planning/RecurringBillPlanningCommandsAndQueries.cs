using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Planning;

public sealed record CreateRecurringBillCommand(
    Guid FamilyId,
    string Name,
    string Merchant,
    decimal Amount,
    string Frequency,
    int DayOfMonth,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive) : ICommand<RecurringBillDetails>;

public sealed class CreateRecurringBillCommandHandler(
    IRecurringBillService recurringBillService) : ICommandHandler<CreateRecurringBillCommand, RecurringBillDetails>
{
    public Task<RecurringBillDetails> HandleAsync(
        CreateRecurringBillCommand command,
        CancellationToken cancellationToken = default)
    {
        return recurringBillService.CreateAsync(
            command.FamilyId,
            command.Name,
            command.Merchant,
            command.Amount,
            command.Frequency,
            command.DayOfMonth,
            command.StartDate,
            command.EndDate,
            command.IsActive,
            cancellationToken);
    }
}

public sealed record ListRecurringBillsByFamilyQuery(Guid FamilyId) : IQuery<IReadOnlyList<RecurringBillDetails>>;

public sealed class ListRecurringBillsByFamilyQueryHandler(
    IRecurringBillService recurringBillService) : IQueryHandler<ListRecurringBillsByFamilyQuery, IReadOnlyList<RecurringBillDetails>>
{
    public Task<IReadOnlyList<RecurringBillDetails>> HandleAsync(
        ListRecurringBillsByFamilyQuery query,
        CancellationToken cancellationToken = default)
    {
        return recurringBillService.ListByFamilyAsync(query.FamilyId, cancellationToken);
    }
}

public sealed record UpdateRecurringBillCommand(
    Guid RecurringBillId,
    string Name,
    string Merchant,
    decimal Amount,
    string Frequency,
    int DayOfMonth,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive) : ICommand<RecurringBillDetails>;

public sealed class UpdateRecurringBillCommandHandler(
    IRecurringBillService recurringBillService) : ICommandHandler<UpdateRecurringBillCommand, RecurringBillDetails>
{
    public Task<RecurringBillDetails> HandleAsync(
        UpdateRecurringBillCommand command,
        CancellationToken cancellationToken = default)
    {
        return recurringBillService.UpdateAsync(
            command.RecurringBillId,
            command.Name,
            command.Merchant,
            command.Amount,
            command.Frequency,
            command.DayOfMonth,
            command.StartDate,
            command.EndDate,
            command.IsActive,
            cancellationToken);
    }
}

public sealed record DeleteRecurringBillCommand(Guid RecurringBillId) : ICommand<bool>;

public sealed class DeleteRecurringBillCommandHandler(
    IRecurringBillService recurringBillService) : ICommandHandler<DeleteRecurringBillCommand, bool>
{
    public async Task<bool> HandleAsync(
        DeleteRecurringBillCommand command,
        CancellationToken cancellationToken = default)
    {
        await recurringBillService.DeleteAsync(command.RecurringBillId, cancellationToken);
        return true;
    }
}

public sealed record ProjectRecurringBillsQuery(
    Guid FamilyId,
    DateOnly From,
    DateOnly To) : IQuery<IReadOnlyList<RecurringBillProjectionItemDetails>>;

public sealed class ProjectRecurringBillsQueryHandler(
    IRecurringBillService recurringBillService) : IQueryHandler<ProjectRecurringBillsQuery, IReadOnlyList<RecurringBillProjectionItemDetails>>
{
    public Task<IReadOnlyList<RecurringBillProjectionItemDetails>> HandleAsync(
        ProjectRecurringBillsQuery query,
        CancellationToken cancellationToken = default)
    {
        return recurringBillService.ProjectAsync(
            query.FamilyId,
            query.From,
            query.To,
            cancellationToken);
    }
}

public sealed record ListRecurringBillExecutionsQuery(
    Guid RecurringBillId,
    int Take,
    string? Result,
    DateOnly? FromDate,
    DateOnly? ToDate) : IQuery<IReadOnlyList<RecurringBillExecutionDetails>>;

public sealed class ListRecurringBillExecutionsQueryHandler(
    IRecurringBillService recurringBillService) : IQueryHandler<ListRecurringBillExecutionsQuery, IReadOnlyList<RecurringBillExecutionDetails>>
{
    public Task<IReadOnlyList<RecurringBillExecutionDetails>> HandleAsync(
        ListRecurringBillExecutionsQuery query,
        CancellationToken cancellationToken = default)
    {
        return recurringBillService.ListExecutionsAsync(
            query.RecurringBillId,
            query.Take,
            query.Result,
            query.FromDate,
            query.ToDate,
            cancellationToken);
    }
}
