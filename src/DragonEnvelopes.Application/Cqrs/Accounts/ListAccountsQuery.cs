using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Cqrs.Accounts;

public sealed record ListAccountsQuery(Guid? FamilyId) : IQuery<IReadOnlyList<AccountDetails>>;
