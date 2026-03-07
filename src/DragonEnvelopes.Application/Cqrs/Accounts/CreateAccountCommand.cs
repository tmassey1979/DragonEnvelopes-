using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Cqrs.Accounts;

public sealed record CreateAccountCommand(
    Guid FamilyId,
    string Name,
    string Type,
    decimal OpeningBalance) : ICommand<AccountDetails>;
