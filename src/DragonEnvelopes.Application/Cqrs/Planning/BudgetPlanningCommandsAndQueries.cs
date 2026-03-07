using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Planning;

public sealed record CreateBudgetCommand(
    Guid FamilyId,
    string Month,
    decimal TotalIncome) : ICommand<BudgetDetails>;

public sealed class CreateBudgetCommandHandler(
    IBudgetService budgetService) : ICommandHandler<CreateBudgetCommand, BudgetDetails>
{
    public Task<BudgetDetails> HandleAsync(
        CreateBudgetCommand command,
        CancellationToken cancellationToken = default)
    {
        return budgetService.CreateAsync(
            command.FamilyId,
            command.Month,
            command.TotalIncome,
            cancellationToken);
    }
}

public sealed record GetBudgetByMonthQuery(
    Guid FamilyId,
    string Month) : IQuery<BudgetDetails?>;

public sealed class GetBudgetByMonthQueryHandler(
    IBudgetService budgetService) : IQueryHandler<GetBudgetByMonthQuery, BudgetDetails?>
{
    public Task<BudgetDetails?> HandleAsync(
        GetBudgetByMonthQuery query,
        CancellationToken cancellationToken = default)
    {
        return budgetService.GetByMonthAsync(
            query.FamilyId,
            query.Month,
            cancellationToken);
    }
}

public sealed record UpdateBudgetCommand(
    Guid BudgetId,
    decimal TotalIncome) : ICommand<BudgetDetails>;

public sealed class UpdateBudgetCommandHandler(
    IBudgetService budgetService) : ICommandHandler<UpdateBudgetCommand, BudgetDetails>
{
    public Task<BudgetDetails> HandleAsync(
        UpdateBudgetCommand command,
        CancellationToken cancellationToken = default)
    {
        return budgetService.UpdateAsync(
            command.BudgetId,
            command.TotalIncome,
            cancellationToken);
    }
}

public sealed record PreviewEnvelopeRolloverQuery(
    Guid FamilyId,
    string Month) : IQuery<EnvelopeRolloverPreviewDetails>;

public sealed class PreviewEnvelopeRolloverQueryHandler(
    IEnvelopeRolloverService envelopeRolloverService) : IQueryHandler<PreviewEnvelopeRolloverQuery, EnvelopeRolloverPreviewDetails>
{
    public Task<EnvelopeRolloverPreviewDetails> HandleAsync(
        PreviewEnvelopeRolloverQuery query,
        CancellationToken cancellationToken = default)
    {
        return envelopeRolloverService.PreviewAsync(
            query.FamilyId,
            query.Month,
            cancellationToken);
    }
}

public sealed record ApplyEnvelopeRolloverCommand(
    Guid FamilyId,
    string Month,
    string? ActorUserId) : ICommand<EnvelopeRolloverApplyDetails>;

public sealed class ApplyEnvelopeRolloverCommandHandler(
    IEnvelopeRolloverService envelopeRolloverService) : ICommandHandler<ApplyEnvelopeRolloverCommand, EnvelopeRolloverApplyDetails>
{
    public Task<EnvelopeRolloverApplyDetails> HandleAsync(
        ApplyEnvelopeRolloverCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopeRolloverService.ApplyAsync(
            command.FamilyId,
            command.Month,
            command.ActorUserId,
            cancellationToken);
    }
}
