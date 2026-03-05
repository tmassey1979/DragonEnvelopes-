namespace DragonEnvelopes.Domain.Entities;

public sealed class Family
{
    private readonly List<FamilyMember> _members = [];
    private readonly List<Account> _accounts = [];
    private readonly List<Envelope> _envelopes = [];

    public Guid Id { get; }
    public string Name { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public IReadOnlyCollection<FamilyMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<Account> Accounts => _accounts.AsReadOnly();
    public IReadOnlyCollection<Envelope> Envelopes => _envelopes.AsReadOnly();

    public Family(Guid id, string name, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        Id = id;
        Name = ValidateText(name, "Family name");
        CreatedAt = createdAt;
    }

    public void Rename(string name)
    {
        Name = ValidateText(name, "Family name");
    }

    public void AddMember(FamilyMember member)
    {
        if (member.FamilyId != Id)
        {
            throw new DomainValidationException("Member must belong to the same family.");
        }

        if (_members.Any(x => x.KeycloakUserId.Equals(member.KeycloakUserId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainValidationException("A member with the same Keycloak user id already exists.");
        }

        _members.Add(member);
    }

    public void AddAccount(Account account)
    {
        if (account.FamilyId != Id)
        {
            throw new DomainValidationException("Account must belong to the same family.");
        }

        if (_accounts.Any(x => x.Name.Equals(account.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainValidationException("An account with the same name already exists.");
        }

        _accounts.Add(account);
    }

    public void AddEnvelope(Envelope envelope)
    {
        if (envelope.FamilyId != Id)
        {
            throw new DomainValidationException("Envelope must belong to the same family.");
        }

        if (_envelopes.Any(x => x.Name.Equals(envelope.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainValidationException("An envelope with the same name already exists.");
        }

        _envelopes.Add(envelope);
    }

    private static string ValidateText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }
}

