using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class AccountService(IAccountRepository accountRepository) : IAccountService
{
    public async Task<AccountDetails> CreateAsync(
        Guid familyId,
        string name,
        string type,
        decimal openingBalance,
        CancellationToken cancellationToken = default)
    {
        if (!await accountRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        var normalizedName = string.IsNullOrWhiteSpace(name)
            ? string.Empty
            : name.Trim();

        if (await accountRepository.AccountNameExistsAsync(familyId, normalizedName, cancellationToken))
        {
            throw new DomainValidationException("An account with the same name already exists.");
        }

        if (!Enum.TryParse<AccountType>(type, ignoreCase: true, out var parsedType))
        {
            throw new DomainValidationException("Account type is invalid.");
        }

        var account = new Account(
            Guid.NewGuid(),
            familyId,
            normalizedName,
            parsedType,
            Money.FromDecimal(openingBalance).EnsureNonNegative("OpeningBalance"));

        await accountRepository.AddAccountAsync(account, cancellationToken);
        return Map(account);
    }

    public async Task<IReadOnlyList<AccountDetails>> ListAsync(
        Guid? familyId,
        CancellationToken cancellationToken = default)
    {
        var accounts = await accountRepository.ListAccountsAsync(familyId, cancellationToken);
        return accounts.Select(Map).ToArray();
    }

    private static AccountDetails Map(Account account)
    {
        return new AccountDetails(
            account.Id,
            account.FamilyId,
            account.Name,
            account.Type.ToString(),
            account.Balance.Amount);
    }
}
