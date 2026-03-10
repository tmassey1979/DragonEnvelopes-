using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Financial;

public sealed record CreateStripeSetupIntentCommand(
    Guid FamilyId,
    string Email,
    string? Name) : ICommand<StripeSetupIntentDetails>;

public sealed class CreateStripeSetupIntentCommandHandler(
    IFinancialIntegrationService financialIntegrationService) : ICommandHandler<CreateStripeSetupIntentCommand, StripeSetupIntentDetails>
{
    public Task<StripeSetupIntentDetails> HandleAsync(
        CreateStripeSetupIntentCommand command,
        CancellationToken cancellationToken = default)
    {
        return financialIntegrationService.CreateStripeSetupIntentAsync(
            command.FamilyId,
            command.Email,
            command.Name,
            cancellationToken);
    }
}

public sealed record LinkStripeEnvelopeFinancialAccountCommand(
    Guid FamilyId,
    Guid EnvelopeId,
    string? DisplayName) : ICommand<EnvelopeFinancialAccountDetails>;

public sealed class LinkStripeEnvelopeFinancialAccountCommandHandler(
    IEnvelopeFinancialAccountService envelopeFinancialAccountService) : ICommandHandler<LinkStripeEnvelopeFinancialAccountCommand, EnvelopeFinancialAccountDetails>
{
    public Task<EnvelopeFinancialAccountDetails> HandleAsync(
        LinkStripeEnvelopeFinancialAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        return envelopeFinancialAccountService.LinkStripeFinancialAccountAsync(
            command.FamilyId,
            command.EnvelopeId,
            command.DisplayName,
            cancellationToken);
    }
}

public sealed record GetEnvelopeFinancialAccountQuery(
    Guid FamilyId,
    Guid EnvelopeId) : IQuery<EnvelopeFinancialAccountDetails?>;

public sealed class GetEnvelopeFinancialAccountQueryHandler(
    IEnvelopeFinancialAccountService envelopeFinancialAccountService) : IQueryHandler<GetEnvelopeFinancialAccountQuery, EnvelopeFinancialAccountDetails?>
{
    public Task<EnvelopeFinancialAccountDetails?> HandleAsync(
        GetEnvelopeFinancialAccountQuery query,
        CancellationToken cancellationToken = default)
    {
        return envelopeFinancialAccountService.GetByEnvelopeAsync(
            query.FamilyId,
            query.EnvelopeId,
            cancellationToken);
    }
}

public sealed record ListFamilyFinancialAccountsQuery(Guid FamilyId) : IQuery<IReadOnlyList<EnvelopeFinancialAccountDetails>>;

public sealed class ListFamilyFinancialAccountsQueryHandler(
    IEnvelopeFinancialAccountService envelopeFinancialAccountService) : IQueryHandler<ListFamilyFinancialAccountsQuery, IReadOnlyList<EnvelopeFinancialAccountDetails>>
{
    public Task<IReadOnlyList<EnvelopeFinancialAccountDetails>> HandleAsync(
        ListFamilyFinancialAccountsQuery query,
        CancellationToken cancellationToken = default)
    {
        return envelopeFinancialAccountService.ListByFamilyAsync(query.FamilyId, cancellationToken);
    }
}

public sealed record ProcessStripeWebhookCommand(
    string Payload,
    string? SignatureHeader) : ICommand<StripeWebhookProcessResult>;

public sealed class ProcessStripeWebhookCommandHandler(
    IStripeWebhookService stripeWebhookService) : ICommandHandler<ProcessStripeWebhookCommand, StripeWebhookProcessResult>
{
    public Task<StripeWebhookProcessResult> HandleAsync(
        ProcessStripeWebhookCommand command,
        CancellationToken cancellationToken = default)
    {
        return stripeWebhookService.ProcessAsync(
            command.Payload,
            command.SignatureHeader,
            cancellationToken);
    }
}

public sealed record ReplayStripeWebhookEventCommand(
    Guid FamilyId,
    Guid WebhookEventId) : ICommand<StripeWebhookReplayResult>;

public sealed class ReplayStripeWebhookEventCommandHandler(
    IStripeWebhookService stripeWebhookService) : ICommandHandler<ReplayStripeWebhookEventCommand, StripeWebhookReplayResult>
{
    public Task<StripeWebhookReplayResult> HandleAsync(
        ReplayStripeWebhookEventCommand command,
        CancellationToken cancellationToken = default)
    {
        return stripeWebhookService.ReplayFailedEventAsync(
            command.FamilyId,
            command.WebhookEventId,
            cancellationToken);
    }
}

public sealed record GetFamilyFinancialStatusQuery(Guid FamilyId) : IQuery<FamilyFinancialProfileDetails>;

public sealed class GetFamilyFinancialStatusQueryHandler(
    IFamilyFinancialStatusQueryService financialStatusQueryService) : IQueryHandler<GetFamilyFinancialStatusQuery, FamilyFinancialProfileDetails>
{
    public Task<FamilyFinancialProfileDetails> HandleAsync(
        GetFamilyFinancialStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        return financialStatusQueryService.GetStatusAsync(query.FamilyId, cancellationToken);
    }
}

public sealed record UpdateReconciliationDriftThresholdCommand(
    Guid FamilyId,
    decimal ReconciliationDriftThreshold) : ICommand<FamilyFinancialProfileDetails>;

public sealed class UpdateReconciliationDriftThresholdCommandHandler(
    IFinancialIntegrationService financialIntegrationService) : ICommandHandler<UpdateReconciliationDriftThresholdCommand, FamilyFinancialProfileDetails>
{
    public Task<FamilyFinancialProfileDetails> HandleAsync(
        UpdateReconciliationDriftThresholdCommand command,
        CancellationToken cancellationToken = default)
    {
        return financialIntegrationService.UpdateReconciliationDriftThresholdAsync(
            command.FamilyId,
            command.ReconciliationDriftThreshold,
            cancellationToken);
    }
}

public sealed record RewrapProviderSecretsCommand(Guid FamilyId) : ICommand<ProviderSecretsRewrapDetails>;

public sealed class RewrapProviderSecretsCommandHandler(
    IFinancialIntegrationService financialIntegrationService) : ICommandHandler<RewrapProviderSecretsCommand, ProviderSecretsRewrapDetails>
{
    public Task<ProviderSecretsRewrapDetails> HandleAsync(
        RewrapProviderSecretsCommand command,
        CancellationToken cancellationToken = default)
    {
        return financialIntegrationService.RewrapProviderSecretsAsync(
            command.FamilyId,
            cancellationToken);
    }
}
