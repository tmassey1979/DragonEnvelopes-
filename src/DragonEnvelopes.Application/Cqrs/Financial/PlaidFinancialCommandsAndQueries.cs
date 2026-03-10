using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Financial;

public sealed record CreatePlaidLinkTokenCommand(
    Guid FamilyId,
    string? ClientUserId,
    string? ClientName) : ICommand<PlaidLinkTokenDetails>;

public sealed class CreatePlaidLinkTokenCommandHandler(
    IFinancialIntegrationService financialIntegrationService) : ICommandHandler<CreatePlaidLinkTokenCommand, PlaidLinkTokenDetails>
{
    public Task<PlaidLinkTokenDetails> HandleAsync(
        CreatePlaidLinkTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        return financialIntegrationService.CreatePlaidLinkTokenAsync(
            command.FamilyId,
            command.ClientUserId,
            command.ClientName,
            cancellationToken);
    }
}

public sealed record ExchangePlaidPublicTokenCommand(
    Guid FamilyId,
    string PublicToken) : ICommand<FamilyFinancialProfileDetails>;

public sealed class ExchangePlaidPublicTokenCommandHandler(
    IFinancialIntegrationService financialIntegrationService) : ICommandHandler<ExchangePlaidPublicTokenCommand, FamilyFinancialProfileDetails>
{
    public Task<FamilyFinancialProfileDetails> HandleAsync(
        ExchangePlaidPublicTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        return financialIntegrationService.ExchangePlaidPublicTokenAsync(
            command.FamilyId,
            command.PublicToken,
            cancellationToken);
    }
}

public sealed record UpsertPlaidAccountLinkCommand(
    Guid FamilyId,
    Guid AccountId,
    string PlaidAccountId) : ICommand<PlaidAccountLinkDetails>;

public sealed class UpsertPlaidAccountLinkCommandHandler(
    IPlaidTransactionSyncService plaidTransactionSyncService) : ICommandHandler<UpsertPlaidAccountLinkCommand, PlaidAccountLinkDetails>
{
    public Task<PlaidAccountLinkDetails> HandleAsync(
        UpsertPlaidAccountLinkCommand command,
        CancellationToken cancellationToken = default)
    {
        return plaidTransactionSyncService.UpsertAccountLinkAsync(
            command.FamilyId,
            command.AccountId,
            command.PlaidAccountId,
            cancellationToken);
    }
}

public sealed record ListPlaidAccountLinksQuery(Guid FamilyId) : IQuery<IReadOnlyList<PlaidAccountLinkDetails>>;

public sealed class ListPlaidAccountLinksQueryHandler(
    IPlaidTransactionSyncService plaidTransactionSyncService) : IQueryHandler<ListPlaidAccountLinksQuery, IReadOnlyList<PlaidAccountLinkDetails>>
{
    public Task<IReadOnlyList<PlaidAccountLinkDetails>> HandleAsync(
        ListPlaidAccountLinksQuery query,
        CancellationToken cancellationToken = default)
    {
        return plaidTransactionSyncService.ListAccountLinksAsync(query.FamilyId, cancellationToken);
    }
}

public sealed record DeletePlaidAccountLinkCommand(
    Guid FamilyId,
    Guid LinkId) : ICommand<bool>;

public sealed class DeletePlaidAccountLinkCommandHandler(
    IPlaidTransactionSyncService plaidTransactionSyncService) : ICommandHandler<DeletePlaidAccountLinkCommand, bool>
{
    public async Task<bool> HandleAsync(
        DeletePlaidAccountLinkCommand command,
        CancellationToken cancellationToken = default)
    {
        await plaidTransactionSyncService.DeleteAccountLinkAsync(
            command.FamilyId,
            command.LinkId,
            cancellationToken);
        return true;
    }
}

public sealed record SyncPlaidTransactionsCommand(Guid FamilyId) : ICommand<PlaidTransactionSyncDetails>;

public sealed class SyncPlaidTransactionsCommandHandler(
    IPlaidTransactionSyncService plaidTransactionSyncService) : ICommandHandler<SyncPlaidTransactionsCommand, PlaidTransactionSyncDetails>
{
    public Task<PlaidTransactionSyncDetails> HandleAsync(
        SyncPlaidTransactionsCommand command,
        CancellationToken cancellationToken = default)
    {
        return plaidTransactionSyncService.SyncFamilyAsync(command.FamilyId, cancellationToken);
    }
}

public sealed record RefreshPlaidBalancesCommand(Guid FamilyId) : ICommand<PlaidBalanceRefreshDetails>;

public sealed class RefreshPlaidBalancesCommandHandler(
    IPlaidBalanceReconciliationService plaidBalanceReconciliationService) : ICommandHandler<RefreshPlaidBalancesCommand, PlaidBalanceRefreshDetails>
{
    public Task<PlaidBalanceRefreshDetails> HandleAsync(
        RefreshPlaidBalancesCommand command,
        CancellationToken cancellationToken = default)
    {
        return plaidBalanceReconciliationService.RefreshFamilyBalancesAsync(
            command.FamilyId,
            cancellationToken);
    }
}

public sealed record GetPlaidReconciliationReportQuery(Guid FamilyId) : IQuery<PlaidReconciliationReportDetails>;

public sealed class GetPlaidReconciliationReportQueryHandler(
    IPlaidBalanceReconciliationService plaidBalanceReconciliationService) : IQueryHandler<GetPlaidReconciliationReportQuery, PlaidReconciliationReportDetails>
{
    public Task<PlaidReconciliationReportDetails> HandleAsync(
        GetPlaidReconciliationReportQuery query,
        CancellationToken cancellationToken = default)
    {
        return plaidBalanceReconciliationService.GetReconciliationReportAsync(
            query.FamilyId,
            cancellationToken);
    }
}
