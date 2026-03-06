namespace DragonEnvelopes.Application.DTOs;

public sealed record OnboardingProfileDetails(
    Guid Id,
    Guid FamilyId,
    bool AccountsCompleted,
    bool EnvelopesCompleted,
    bool BudgetCompleted,
    bool IsCompleted,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc);
