using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public interface ITransactionsDataService
{
    Task<IReadOnlyList<AccountListItemViewModel>> GetAccountsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionListItemViewModel>> GetTransactionsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);
}
