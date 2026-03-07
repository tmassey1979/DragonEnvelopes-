using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Accounts;

public sealed class ListAccountsQueryHandler(
    IAccountService accountService) : IQueryHandler<ListAccountsQuery, IReadOnlyList<AccountDetails>>
{
    public Task<IReadOnlyList<AccountDetails>> HandleAsync(
        ListAccountsQuery query,
        CancellationToken cancellationToken = default)
    {
        return accountService.ListAsync(query.FamilyId, cancellationToken);
    }
}
