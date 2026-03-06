using DragonEnvelopes.Desktop.Services;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Tests;

internal sealed class FakeAccountsDataService(Guid accountId) : IAccountsDataService
{
    public Task<IReadOnlyList<AccountListItemViewModel>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AccountListItemViewModel> accounts =
        [
            new AccountListItemViewModel(accountId, "Primary Checking", "Checking", "$1,250.00")
        ];

        return Task.FromResult(accounts);
    }

    public Task CreateAccountAsync(string name, string type, decimal openingBalance, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not required for smoke tests.");
    }
}

internal sealed class FakeEnvelopesDataService(Guid envelopeId) : IEnvelopesDataService
{
    public Task<IReadOnlyList<EnvelopeListItemViewModel>> GetEnvelopesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EnvelopeListItemViewModel> envelopes =
        [
            new EnvelopeListItemViewModel(envelopeId, "Groceries", 350m, 150m, isArchived: false)
        ];

        return Task.FromResult(envelopes);
    }

    public Task<EnvelopeListItemViewModel> CreateEnvelopeAsync(string name, decimal monthlyBudget, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not required for smoke tests.");
    }

    public Task<EnvelopeListItemViewModel> UpdateEnvelopeAsync(
        Guid envelopeId,
        string name,
        decimal monthlyBudget,
        bool isArchived,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not required for smoke tests.");
    }

    public Task<EnvelopeListItemViewModel> ArchiveEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not required for smoke tests.");
    }
}
