namespace DragonEnvelopes.Domain.Entities;

public sealed class OnboardingProfile
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public bool AccountsCompleted { get; private set; }
    public bool EnvelopesCompleted { get; private set; }
    public bool BudgetCompleted { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public OnboardingProfile(
        Guid id,
        Guid familyId,
        bool accountsCompleted,
        bool envelopesCompleted,
        bool budgetCompleted,
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
        AccountsCompleted = accountsCompleted;
        EnvelopesCompleted = envelopesCompleted;
        BudgetCompleted = budgetCompleted;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        CompletedAtUtc = completedAtUtc;
    }

    public bool IsCompleted => AccountsCompleted && EnvelopesCompleted && BudgetCompleted;

    public void UpdateMilestones(
        bool accountsCompleted,
        bool envelopesCompleted,
        bool budgetCompleted,
        DateTimeOffset updatedAtUtc)
    {
        AccountsCompleted = accountsCompleted;
        EnvelopesCompleted = envelopesCompleted;
        BudgetCompleted = budgetCompleted;
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
