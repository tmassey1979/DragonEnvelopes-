using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Accounts;

public sealed class CreateAccountCommandHandler(
    IAccountService accountService) : ICommandHandler<CreateAccountCommand, AccountDetails>
{
    public Task<AccountDetails> HandleAsync(
        CreateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        return accountService.CreateAsync(
            command.FamilyId,
            command.Name,
            command.Type,
            command.OpeningBalance,
            cancellationToken);
    }
}
