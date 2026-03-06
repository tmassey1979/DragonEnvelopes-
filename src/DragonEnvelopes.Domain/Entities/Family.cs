namespace DragonEnvelopes.Domain.Entities;

public sealed class Family
{
    public const string DefaultCurrencyCode = "USD";
    public const string DefaultTimeZoneId = "America/Chicago";

    private readonly List<FamilyMember> _members = [];
    private readonly List<Account> _accounts = [];
    private readonly List<Envelope> _envelopes = [];

    public Guid Id { get; }
    public string Name { get; private set; }
    public string CurrencyCode { get; private set; }
    public string TimeZoneId { get; private set; }
    public string? PayFrequency { get; private set; }
    public string? BudgetingStyle { get; private set; }
    public decimal? HouseholdMonthlyIncome { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<FamilyMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<Account> Accounts => _accounts.AsReadOnly();
    public IReadOnlyCollection<Envelope> Envelopes => _envelopes.AsReadOnly();

    public Family(Guid id, string name, DateTimeOffset createdAt)
        : this(
            id,
            name,
            createdAt,
            DefaultCurrencyCode,
            DefaultTimeZoneId,
            createdAt)
    {
    }

    public Family(
        Guid id,
        string name,
        DateTimeOffset createdAt,
        string currencyCode,
        string timeZoneId,
        DateTimeOffset updatedAt)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        Id = id;
        Name = ValidateText(name, "Family name");
        CurrencyCode = ValidateCurrencyCode(currencyCode);
        TimeZoneId = ValidateTimeZoneId(timeZoneId);
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public void Rename(string name)
    {
        Name = ValidateText(name, "Family name");
    }

    public void UpdateProfile(
        string name,
        string currencyCode,
        string timeZoneId,
        DateTimeOffset updatedAt)
    {
        Name = ValidateText(name, "Family name");
        CurrencyCode = ValidateCurrencyCode(currencyCode);
        TimeZoneId = ValidateTimeZoneId(timeZoneId);
        UpdatedAt = updatedAt;
    }

    public void UpdateBudgetPreferences(
        string payFrequency,
        string budgetingStyle,
        decimal? householdMonthlyIncome,
        DateTimeOffset updatedAt)
    {
        PayFrequency = ValidatePayFrequency(payFrequency);
        BudgetingStyle = ValidateBudgetingStyle(budgetingStyle);
        HouseholdMonthlyIncome = ValidateHouseholdMonthlyIncome(householdMonthlyIncome);
        UpdatedAt = updatedAt;
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

    private static string ValidateCurrencyCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("Currency code is required.");
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || !normalized.All(static c => char.IsAsciiLetter(c)))
        {
            throw new DomainValidationException("Currency code must be a 3-letter code.");
        }

        return normalized;
    }

    private static string ValidateTimeZoneId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("Time zone is required.");
        }

        return value.Trim();
    }

    private static string ValidatePayFrequency(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("Pay frequency is required.");
        }

        var normalized = value.Trim();
        return normalized switch
        {
            "Weekly" => normalized,
            "BiWeekly" => normalized,
            "SemiMonthly" => normalized,
            "Monthly" => normalized,
            _ => throw new DomainValidationException("Pay frequency is invalid.")
        };
    }

    private static string ValidateBudgetingStyle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("Budgeting style is required.");
        }

        var normalized = value.Trim();
        return normalized switch
        {
            "ZeroBased" => normalized,
            "EnvelopePriority" => normalized,
            _ => throw new DomainValidationException("Budgeting style is invalid.")
        };
    }

    private static decimal? ValidateHouseholdMonthlyIncome(decimal? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (value.Value < 0m)
        {
            throw new DomainValidationException("Household monthly income cannot be negative.");
        }

        return value.Value;
    }
}
