using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public interface IAccountsDataService
{
    Task<IReadOnlyList<AccountListItemViewModel>> GetAccountsAsync(CancellationToken cancellationToken = default);

    Task CreateAccountAsync(
        string name,
        string type,
        decimal openingBalance,
        CancellationToken cancellationToken = default);
}
