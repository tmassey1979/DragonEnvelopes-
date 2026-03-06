namespace DragonEnvelopes.Domain.Entities;

public sealed class OnboardingProfile
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public bool MembersCompleted { get; private set; }
    public bool AccountsCompleted { get; private set; }
    public bool EnvelopesCompleted { get; private set; }
    public bool BudgetCompleted { get; private set; }
    public bool PlaidCompleted { get; private set; }
    public bool StripeAccountsCompleted { get; private set; }
    public bool CardsCompleted { get; private set; }
    public bool AutomationCompleted { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public OnboardingProfile(
        Guid id,
        Guid familyId,
        bool membersCompleted,
        bool accountsCompleted,
        bool envelopesCompleted,
        bool budgetCompleted,
        bool plaidCompleted,
        bool stripeAccountsCompleted,
        bool cardsCompleted,
        bool automationCompleted,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? completedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Onboarding profile id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        Id = id;
        FamilyId = familyId;
        MembersCompleted = membersCompleted;
        AccountsCompleted = accountsCompleted;
        EnvelopesCompleted = envelopesCompleted;
        BudgetCompleted = budgetCompleted;
        PlaidCompleted = plaidCompleted;
        StripeAccountsCompleted = stripeAccountsCompleted;
        CardsCompleted = cardsCompleted;
        AutomationCompleted = automationCompleted;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        CompletedAtUtc = completedAtUtc;
    }

    public bool IsCompleted => MembersCompleted
                               && AccountsCompleted
                               && EnvelopesCompleted
                               && BudgetCompleted
                               && PlaidCompleted
                               && StripeAccountsCompleted
                               && CardsCompleted
                               && AutomationCompleted;

    public void UpdateMilestones(
        bool membersCompleted,
        bool accountsCompleted,
        bool envelopesCompleted,
        bool budgetCompleted,
        bool plaidCompleted,
        bool stripeAccountsCompleted,
        bool cardsCompleted,
        bool automationCompleted,
        DateTimeOffset updatedAtUtc)
    {
        MembersCompleted = membersCompleted;
        AccountsCompleted = accountsCompleted;
        EnvelopesCompleted = envelopesCompleted;
        BudgetCompleted = budgetCompleted;
        PlaidCompleted = plaidCompleted;
        StripeAccountsCompleted = stripeAccountsCompleted;
        CardsCompleted = cardsCompleted;
        AutomationCompleted = automationCompleted;
        UpdatedAtUtc = updatedAtUtc;

        if (IsCompleted)
        {
            CompletedAtUtc ??= updatedAtUtc;
        }
        else
        {
            CompletedAtUtc = null;
        }
    }
}
