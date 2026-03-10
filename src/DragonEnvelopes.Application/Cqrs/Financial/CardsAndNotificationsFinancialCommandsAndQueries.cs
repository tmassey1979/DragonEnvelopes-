using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Financial;

public sealed record IssueVirtualEnvelopeCardCommand(
    Guid FamilyId,
    Guid EnvelopeId,
    string? CardholderName) : ICommand<EnvelopePaymentCardDetails>;

public sealed class IssueVirtualEnvelopeCardCommandHandler(
    IEnvelopePaymentCardService envelopePaymentCardService) : ICommandHandler<IssueVirtualEnvelopeCardCommand, EnvelopePaymentCardDetails>
{
    public Task<EnvelopePaymentCardDetails> HandleAsync(
        IssueVirtualEnvelopeCardCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopePaymentCardService.IssueVirtualCardAsync(
            command.FamilyId,
            command.EnvelopeId,
            command.CardholderName,
            cancellationToken);
    }
}

public sealed record IssuePhysicalEnvelopeCardCommand(
    Guid FamilyId,
    Guid EnvelopeId,
    string? CardholderName,
    string RecipientName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string StateOrProvince,
    string PostalCode,
    string CountryCode) : ICommand<EnvelopePhysicalCardIssuanceDetails>;

public sealed class IssuePhysicalEnvelopeCardCommandHandler(
    IEnvelopePaymentCardService envelopePaymentCardService) : ICommandHandler<IssuePhysicalEnvelopeCardCommand, EnvelopePhysicalCardIssuanceDetails>
{
    public Task<EnvelopePhysicalCardIssuanceDetails> HandleAsync(
        IssuePhysicalEnvelopeCardCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopePaymentCardService.IssuePhysicalCardAsync(
            command.FamilyId,
            command.EnvelopeId,
            command.CardholderName,
            command.RecipientName,
            command.AddressLine1,
            command.AddressLine2,
            command.City,
            command.StateOrProvince,
            command.PostalCode,
            command.CountryCode,
            cancellationToken);
    }
}

public sealed record ListEnvelopeCardsQuery(
    Guid FamilyId,
    Guid EnvelopeId) : IQuery<IReadOnlyList<EnvelopePaymentCardDetails>>;

public sealed class ListEnvelopeCardsQueryHandler(
    IEnvelopePaymentCardService envelopePaymentCardService) : IQueryHandler<ListEnvelopeCardsQuery, IReadOnlyList<EnvelopePaymentCardDetails>>
{
    public Task<IReadOnlyList<EnvelopePaymentCardDetails>> HandleAsync(
        ListEnvelopeCardsQuery query,
        CancellationToken cancellationToken = default)
    {
        return envelopePaymentCardService.ListByEnvelopeAsync(
            query.FamilyId,
            query.EnvelopeId,
            cancellationToken);
    }
}

public sealed record FreezeEnvelopeCardCommand(
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId) : ICommand<EnvelopePaymentCardDetails>;

public sealed class FreezeEnvelopeCardCommandHandler(
    IEnvelopePaymentCardService envelopePaymentCardService) : ICommandHandler<FreezeEnvelopeCardCommand, EnvelopePaymentCardDetails>
{
    public Task<EnvelopePaymentCardDetails> HandleAsync(
        FreezeEnvelopeCardCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopePaymentCardService.FreezeCardAsync(
            command.FamilyId,
            command.EnvelopeId,
            command.CardId,
            cancellationToken);
    }
}

public sealed record UnfreezeEnvelopeCardCommand(
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId) : ICommand<EnvelopePaymentCardDetails>;

public sealed class UnfreezeEnvelopeCardCommandHandler(
    IEnvelopePaymentCardService envelopePaymentCardService) : ICommandHandler<UnfreezeEnvelopeCardCommand, EnvelopePaymentCardDetails>
{
    public Task<EnvelopePaymentCardDetails> HandleAsync(
        UnfreezeEnvelopeCardCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopePaymentCardService.UnfreezeCardAsync(
            command.FamilyId,
            command.EnvelopeId,
            command.CardId,
            cancellationToken);
    }
}

public sealed record CancelEnvelopeCardCommand(
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId) : ICommand<EnvelopePaymentCardDetails>;

public sealed class CancelEnvelopeCardCommandHandler(
    IEnvelopePaymentCardService envelopePaymentCardService) : ICommandHandler<CancelEnvelopeCardCommand, EnvelopePaymentCardDetails>
{
    public Task<EnvelopePaymentCardDetails> HandleAsync(
        CancelEnvelopeCardCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopePaymentCardService.CancelCardAsync(
            command.FamilyId,
            command.EnvelopeId,
            command.CardId,
            cancellationToken);
    }
}

public sealed record GetEnvelopePhysicalCardIssuanceQuery(
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId) : IQuery<EnvelopePhysicalCardIssuanceDetails?>;

public sealed class GetEnvelopePhysicalCardIssuanceQueryHandler(
    IEnvelopePaymentCardService envelopePaymentCardService) : IQueryHandler<GetEnvelopePhysicalCardIssuanceQuery, EnvelopePhysicalCardIssuanceDetails?>
{
    public Task<EnvelopePhysicalCardIssuanceDetails?> HandleAsync(
        GetEnvelopePhysicalCardIssuanceQuery query,
        CancellationToken cancellationToken = default)
    {
        return envelopePaymentCardService.GetPhysicalCardIssuanceAsync(
            query.FamilyId,
            query.EnvelopeId,
            query.CardId,
            cancellationToken);
    }
}

public sealed record RefreshEnvelopePhysicalCardIssuanceCommand(
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId) : ICommand<EnvelopePhysicalCardIssuanceDetails>;

public sealed class RefreshEnvelopePhysicalCardIssuanceCommandHandler(
    IEnvelopePaymentCardService envelopePaymentCardService) : ICommandHandler<RefreshEnvelopePhysicalCardIssuanceCommand, EnvelopePhysicalCardIssuanceDetails>
{
    public Task<EnvelopePhysicalCardIssuanceDetails> HandleAsync(
        RefreshEnvelopePhysicalCardIssuanceCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopePaymentCardService.RefreshPhysicalCardIssuanceStatusAsync(
            command.FamilyId,
            command.EnvelopeId,
            command.CardId,
            cancellationToken);
    }
}

public sealed record UpsertEnvelopeCardControlsCommand(
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId,
    decimal? DailyLimitAmount,
    IReadOnlyCollection<string>? AllowedMerchantCategories,
    IReadOnlyCollection<string>? AllowedMerchantNames,
    string? ChangedBy) : ICommand<EnvelopePaymentCardControlDetails>;

public sealed class UpsertEnvelopeCardControlsCommandHandler(
    IEnvelopePaymentCardControlService envelopePaymentCardControlService) : ICommandHandler<UpsertEnvelopeCardControlsCommand, EnvelopePaymentCardControlDetails>
{
    public Task<EnvelopePaymentCardControlDetails> HandleAsync(
        UpsertEnvelopeCardControlsCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopePaymentCardControlService.UpsertControlsAsync(
            command.FamilyId,
            command.EnvelopeId,
            command.CardId,
            command.DailyLimitAmount,
            command.AllowedMerchantCategories,
            command.AllowedMerchantNames,
            command.ChangedBy,
            cancellationToken);
    }
}

public sealed record GetEnvelopeCardControlsQuery(
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId) : IQuery<EnvelopePaymentCardControlDetails?>;

public sealed class GetEnvelopeCardControlsQueryHandler(
    IEnvelopePaymentCardControlService envelopePaymentCardControlService) : IQueryHandler<GetEnvelopeCardControlsQuery, EnvelopePaymentCardControlDetails?>
{
    public Task<EnvelopePaymentCardControlDetails?> HandleAsync(
        GetEnvelopeCardControlsQuery query,
        CancellationToken cancellationToken = default)
    {
        return envelopePaymentCardControlService.GetByCardAsync(
            query.FamilyId,
            query.EnvelopeId,
            query.CardId,
            cancellationToken);
    }
}

public sealed record ListEnvelopeCardControlAuditQuery(
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId) : IQuery<IReadOnlyList<EnvelopePaymentCardControlAuditDetails>>;

public sealed class ListEnvelopeCardControlAuditQueryHandler(
    IEnvelopePaymentCardControlService envelopePaymentCardControlService) : IQueryHandler<ListEnvelopeCardControlAuditQuery, IReadOnlyList<EnvelopePaymentCardControlAuditDetails>>
{
    public Task<IReadOnlyList<EnvelopePaymentCardControlAuditDetails>> HandleAsync(
        ListEnvelopeCardControlAuditQuery query,
        CancellationToken cancellationToken = default)
    {
        return envelopePaymentCardControlService.ListAuditByCardAsync(
            query.FamilyId,
            query.EnvelopeId,
            query.CardId,
            cancellationToken);
    }
}

public sealed record EvaluateEnvelopeCardSpendQuery(
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId,
    string MerchantName,
    string? MerchantCategory,
    decimal Amount,
    decimal SpentTodayAmount) : IQuery<CardSpendEvaluationDetails>;

public sealed class EvaluateEnvelopeCardSpendQueryHandler(
    IEnvelopePaymentCardControlService envelopePaymentCardControlService) : IQueryHandler<EvaluateEnvelopeCardSpendQuery, CardSpendEvaluationDetails>
{
    public Task<CardSpendEvaluationDetails> HandleAsync(
        EvaluateEnvelopeCardSpendQuery query,
        CancellationToken cancellationToken = default)
    {
        return envelopePaymentCardControlService.EvaluateSpendAsync(
            query.FamilyId,
            query.EnvelopeId,
            query.CardId,
            query.MerchantName,
            query.MerchantCategory,
            query.Amount,
            query.SpentTodayAmount,
            cancellationToken);
    }
}

public sealed record GetNotificationPreferenceQuery(
    Guid FamilyId,
    string UserId) : IQuery<NotificationPreferenceDetails>;

public sealed class GetNotificationPreferenceQueryHandler(
    IParentSpendNotificationService parentSpendNotificationService) : IQueryHandler<GetNotificationPreferenceQuery, NotificationPreferenceDetails>
{
    public Task<NotificationPreferenceDetails> HandleAsync(
        GetNotificationPreferenceQuery query,
        CancellationToken cancellationToken = default)
    {
        return parentSpendNotificationService.GetPreferenceAsync(
            query.FamilyId,
            query.UserId,
            cancellationToken);
    }
}

public sealed record UpsertNotificationPreferenceCommand(
    Guid FamilyId,
    string UserId,
    bool EmailEnabled,
    bool InAppEnabled,
    bool SmsEnabled) : ICommand<NotificationPreferenceDetails>;

public sealed class UpsertNotificationPreferenceCommandHandler(
    IParentSpendNotificationService parentSpendNotificationService) : ICommandHandler<UpsertNotificationPreferenceCommand, NotificationPreferenceDetails>
{
    public Task<NotificationPreferenceDetails> HandleAsync(
        UpsertNotificationPreferenceCommand command,
        CancellationToken cancellationToken = default)
    {
        return parentSpendNotificationService.UpsertPreferenceAsync(
            command.FamilyId,
            command.UserId,
            command.EmailEnabled,
            command.InAppEnabled,
            command.SmsEnabled,
            cancellationToken);
    }
}

public sealed record ListFailedNotificationDispatchEventsQuery(
    Guid FamilyId,
    int Take) : IQuery<IReadOnlyList<SpendNotificationDispatchEventDetails>>;

public sealed class ListFailedNotificationDispatchEventsQueryHandler(
    ISpendNotificationDispatchService spendNotificationDispatchService) : IQueryHandler<ListFailedNotificationDispatchEventsQuery, IReadOnlyList<SpendNotificationDispatchEventDetails>>
{
    public Task<IReadOnlyList<SpendNotificationDispatchEventDetails>> HandleAsync(
        ListFailedNotificationDispatchEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        return spendNotificationDispatchService.ListFailedEventsAsync(
            query.FamilyId,
            query.Take,
            cancellationToken);
    }
}

public sealed record RetryNotificationDispatchEventCommand(
    Guid FamilyId,
    Guid EventId) : ICommand<SpendNotificationDispatchEventDetails>;

public sealed class RetryNotificationDispatchEventCommandHandler(
    ISpendNotificationDispatchService spendNotificationDispatchService) : ICommandHandler<RetryNotificationDispatchEventCommand, SpendNotificationDispatchEventDetails>
{
    public Task<SpendNotificationDispatchEventDetails> HandleAsync(
        RetryNotificationDispatchEventCommand command,
        CancellationToken cancellationToken = default)
    {
        return spendNotificationDispatchService.RetryFailedEventAsync(
            command.FamilyId,
            command.EventId,
            cancellationToken);
    }
}

public sealed record ReplayNotificationDispatchEventCommand(
    Guid FamilyId,
    Guid EventId) : ICommand<SpendNotificationDispatchEventDetails>;

public sealed class ReplayNotificationDispatchEventCommandHandler(
    ISpendNotificationDispatchService spendNotificationDispatchService) : ICommandHandler<ReplayNotificationDispatchEventCommand, SpendNotificationDispatchEventDetails>
{
    public Task<SpendNotificationDispatchEventDetails> HandleAsync(
        ReplayNotificationDispatchEventCommand command,
        CancellationToken cancellationToken = default)
    {
        return spendNotificationDispatchService.ReplayEventAsync(
            command.FamilyId,
            command.EventId,
            cancellationToken);
    }
}
